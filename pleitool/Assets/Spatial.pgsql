-- Populates the spatial columns from the imported data. This script can
-- be run more than once. Statements are split at semicolons, so they must
-- not contain semicolons except as terminators.

-- place: places without a location have 0,0 as their representative point
-- and bounding box, which must not be mapped to a real position
UPDATE place SET geo_pt=NULL, geo_bbx=NULL;
UPDATE place SET geo_pt=ST_SetSRID(ST_MakePoint(rp_lon,rp_lat), 4326)::geography
WHERE NOT (rp_lat=0 AND rp_lon=0);

-- place bbox: a degenerate box (a single point) becomes a point
UPDATE place SET geo_bbx=(CASE
	WHEN bbox_sw_lon=bbox_ne_lon AND bbox_sw_lat=bbox_ne_lat
	THEN ST_SetSRID(ST_MakePoint(bbox_sw_lon,bbox_sw_lat), 4326)
	ELSE ST_MakeEnvelope(bbox_sw_lon,bbox_sw_lat,bbox_ne_lon,bbox_ne_lat, 4326)
	END)::geography
WHERE bbox_sw_lat IS NOT NULL AND bbox_sw_lon IS NOT NULL
AND bbox_ne_lat IS NOT NULL AND bbox_ne_lon IS NOT NULL
AND NOT (bbox_sw_lat=0 AND bbox_sw_lon=0 AND bbox_ne_lat=0 AND bbox_ne_lon=0);

-- place_feature
UPDATE place_feature SET geometry=NULL WHERE geometry='null';
UPDATE place_feature SET geo=st_force2d(ST_GeomFromGeoJSON(geometry)) WHERE geometry IS NOT NULL;

-- location
UPDATE location SET geometry=NULL WHERE geometry='null';
UPDATE location SET geo=st_force2d(ST_GeomFromGeoJSON(geometry)) WHERE geometry IS NOT NULL;

-- GeoJSON polygons may have unclosed rings, which ST_GeomFromGeoJSON
-- accepts while reading binary data rejects them, so that a bulk dump of
-- these tables could not be restored: repair them
UPDATE place_feature f SET geo=ST_MakeValid(f.geo::geometry)::geography
WHERE EXISTS (
	SELECT 1 FROM ST_Dump(f.geo::geometry) d, ST_DumpRings(d.geom) r
	WHERE GeometryType(d.geom)='POLYGON'
	AND (ST_NPoints(r.geom) < 4 OR NOT ST_IsClosed(ST_ExteriorRing(r.geom)))
);
UPDATE location l SET geo=ST_MakeValid(l.geo::geometry)::geography
WHERE EXISTS (
	SELECT 1 FROM ST_Dump(l.geo::geometry) d, ST_DumpRings(d.geom) r
	WHERE GeometryType(d.geom)='POLYGON'
	AND (ST_NPoints(r.geom) < 4 OR NOT ST_IsClosed(ST_ExteriorRing(r.geom)))
);
