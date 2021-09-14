-- place
UPDATE place SET geo_pt=ST_Point(rp_lon,rp_lat) WHERE rp_lat<>0;
UPDATE place SET geo_bbx=ST_GeomFromText('POLYGON((' || bbox_sw_lon::varchar || ' ' || bbox_sw_lat::varchar || ',' || bbox_ne_lon::varchar || ' ' || bbox_sw_lat::varchar || ',' || bbox_ne_lon::varchar || ' ' || bbox_ne_lat::varchar || ',' || bbox_sw_lon::varchar || ' ' || bbox_ne_lat::varchar || ',' || bbox_sw_lon::varchar || ' ' || bbox_sw_lat::varchar || '))', 4326);

-- place_feature
UPDATE place_feature SET geometry=NULL WHERE geometry='null';
UPDATE place_feature SET geo=(SELECT st_force2d(ST_GeomFromGeoJSON(geometry))) WHERE geometry IS NOT NULL;

-- location
UPDATE location SET geometry=NULL WHERE geometry='null';
UPDATE location SET geo=(SELECT st_force2d(ST_GeomFromGeoJSON(geometry))) WHERE geometry IS NOT NULL;
