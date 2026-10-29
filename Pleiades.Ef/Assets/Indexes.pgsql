-- Pleiades secondary indexes (PostgreSQL 15+ with PostGIS and pg_trgm).
-- This script is idempotent: it is part of the database schema, and can be
-- run on existing databases (e.g. via pleitool pop-spatial).
-- Note: statements must not contain semicolons except as terminators.

-- legacy indexes, superseded by those below
DROP INDEX IF EXISTS lookup_full_name_idx;

-- lookup (a few hundred rows): by group, ordered by group and full name
CREATE INDEX IF NOT EXISTS lookup_group_full_name_idx
	ON lookup ("group", full_name, id);
DROP INDEX IF EXISTS lookup_group_idx;

-- place: browsing by title, and spatial filters. Distances are measured
-- on the geography point, while bounding box filters use the geometry of the
-- place's bounding box, so that boxes are lon/lat rectangles.
CREATE INDEX IF NOT EXISTS place_title_idx ON place (title, id);
CREATE INDEX IF NOT EXISTS place_geo_pt_idx ON place USING gist (geo_pt);
CREATE INDEX IF NOT EXISTS place_geo_bbx_geom_idx
	ON place USING gist ((geo_bbx::geometry));

-- place children
CREATE INDEX IF NOT EXISTS place_meta_place_name_idx
	ON place_meta (place_id, "name") INCLUDE (value);
CREATE INDEX IF NOT EXISTS place_meta_name_value_idx
	ON place_meta ("name", value) INCLUDE (place_id);
CREATE INDEX IF NOT EXISTS place_feature_place_idx ON place_feature (place_id);
CREATE INDEX IF NOT EXISTS place_attestation_place_idx
	ON place_attestation (place_id);
CREATE INDEX IF NOT EXISTS place_reference_place_idx
	ON place_reference (place_id);
CREATE INDEX IF NOT EXISTS place_author_link_author_idx
	ON place_author_link (author_id);
CREATE INDEX IF NOT EXISTS place_link_target_idx ON place_link (target_id);

-- name
CREATE INDEX IF NOT EXISTS name_place_idx ON "name" (place_id);
CREATE INDEX IF NOT EXISTS name_attestation_name_idx
	ON name_attestation (name_id);
CREATE INDEX IF NOT EXISTS name_reference_name_idx ON name_reference (name_id);
CREATE INDEX IF NOT EXISTS name_author_link_author_idx
	ON name_author_link (author_id);

-- location
CREATE INDEX IF NOT EXISTS location_place_idx ON "location" (place_id);
CREATE INDEX IF NOT EXISTS location_geo_idx ON "location" USING gist (geo);
CREATE INDEX IF NOT EXISTS location_attestation_location_idx
	ON location_attestation (location_id);
CREATE INDEX IF NOT EXISTS location_meta_location_idx
	ON location_meta (location_id);
CREATE INDEX IF NOT EXISTS location_reference_location_idx
	ON location_reference (location_id);
CREATE INDEX IF NOT EXISTS location_author_link_author_idx
	ON location_author_link (author_id);

-- connection
CREATE INDEX IF NOT EXISTS connection_source_idx ON "connection" (source_id);
CREATE INDEX IF NOT EXISTS connection_target_idx ON "connection" (target_id);
CREATE INDEX IF NOT EXISTS connection_attestation_connection_idx
	ON connection_attestation (connection_id);
CREATE INDEX IF NOT EXISTS connection_reference_connection_idx
	ON connection_reference (connection_id);
CREATE INDEX IF NOT EXISTS connection_author_link_author_idx
	ON connection_author_link (author_id);
