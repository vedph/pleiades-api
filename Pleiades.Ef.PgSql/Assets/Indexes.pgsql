-- Pleiades Embix index secondary indexes and constraints (PostgreSQL 15+).
-- This script is idempotent, and is run after each bulk load. Index names
-- are the same of Embix, so that the Embix DropIndexes script drops them.

-- legacy indexes, superseded by those below
DROP INDEX IF EXISTS eix_token_value_idx;
DROP INDEX IF EXISTS eix_occurrence_field_idx;

-- token: unique by value and language. This also serves equality lookups
-- by value (the leading column).
CREATE UNIQUE INDEX IF NOT EXISTS eix_token_value_language_uix
	ON eix_token (value, "language") NULLS NOT DISTINCT;

-- token: prefix searches (LIKE 'abc%') whatever the database collation.
CREATE INDEX IF NOT EXISTS eix_token_value_pattern_idx
	ON eix_token (value text_pattern_ops);

-- token: contains, ends-with, wildcards, ILIKE, regex and trigram
-- similarity searches.
CREATE INDEX IF NOT EXISTS eix_token_value_trgm_idx
	ON eix_token USING gin (value gin_trgm_ops);

-- occurrence: FK to token (unless any FK to it is already present).
DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint
		WHERE conrelid = 'eix_occurrence'::regclass AND contype = 'f'
		AND confrelid = 'eix_token'::regclass
	) THEN
		ALTER TABLE eix_occurrence ADD CONSTRAINT eix_occurrence_token_fk
			FOREIGN KEY (token_id) REFERENCES eix_token(id) ON DELETE CASCADE;
	END IF;
END $$;

-- occurrence: the main search path (matching tokens -> occurrences in
-- the requested scopes, with rank and years -> places). Covering, so that
-- quick searches are served by index-only scans.
CREATE INDEX IF NOT EXISTS eix_occurrence_token_field_target_idx
	ON eix_occurrence (token_id, field, target_id)
	INCLUDE ("rank", year_min, year_max);

-- occurrence: lookups by place (searches without text filtered by rank
-- or years), optionally by field.
CREATE INDEX IF NOT EXISTS eix_occurrence_target_field_idx
	ON eix_occurrence (target_id, field)
	INCLUDE ("rank", year_min, year_max);

-- sync the token identity with the client-assigned IDs
SELECT setval(pg_get_serial_sequence('eix_token', 'id'),
	COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM eix_token;

ANALYZE eix_token;
ANALYZE eix_occurrence;
