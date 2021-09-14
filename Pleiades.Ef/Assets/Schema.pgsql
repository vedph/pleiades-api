-- Enable PostGIS (as of 3.0 contains just geometry/geography)
CREATE EXTENSION postgis;
-- enable raster support (for 3+)
CREATE EXTENSION postgis_raster;
-- Enable Topology
CREATE EXTENSION postgis_topology;
-- Enable PostGIS Advanced 3D
-- and other geoprocessing algorithms
-- sfcgal not available with all distributions
CREATE EXTENSION postgis_sfcgal;

-- lookup
CREATE TABLE public.lookup (
	id integer NOT NULL,
	full_name varchar(200) NOT NULL,
	short_name varchar(100) NULL,
	"group" varchar(50) NULL,
	CONSTRAINT lookup_pk PRIMARY KEY (id)
);
CREATE INDEX lookup_full_name_idx ON public.lookup (full_name);
CREATE INDEX lookup_group_idx ON public.lookup ("group");

-- author
CREATE TABLE public.author (
	id varchar(50) NOT NULL,
	name varchar(100) NOT NULL,
	homepage varchar(200) NULL,
	CONSTRAINT author_pk PRIMARY KEY (id)
);

-- place
CREATE TABLE public.place (
	id varchar(20) NOT NULL,
	uri varchar(200) NOT NULL,
	title varchar(200) NOT NULL,
	description varchar(5000) NULL,
	details text NULL,
	provenance varchar(500) NULL,
	rights varchar(500) NULL,
	review_state_id integer NOT NULL,
	created date NOT NULL,
	modified date NOT NULL,
	rp_lat double precision NOT NULL,
	rp_lon double precision NOT NULL,
	bbox_sw_lat double precision NULL,
	bbox_sw_lon double precision NULL,
	bbox_ne_lat double precision NULL,
	bbox_ne_lon double precision NULL,
	geo_pt geography NULL,
	geo_bbx geography NULL,
	CONSTRAINT place_pk PRIMARY KEY (id)
);

-- placeAttestation
CREATE TABLE public.place_attestation (
	id serial NOT NULL,
	place_id varchar(20) NOT NULL,
	period_id integer NOT NULL,
	confidence_id integer NOT NULL,
	"rank" smallint NOT NULL,
	CONSTRAINT place_attestation_pk PRIMARY KEY (id),
	CONSTRAINT place_attestation_fk FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- placeAuthorLink
CREATE TABLE public.place_author_link (
	place_id varchar(20) NOT NULL,
	author_id varchar(50) NOT NULL,
	"role" char(1) NOT NULL,
	CONSTRAINT place_author_link_pk PRIMARY KEY (place_id,author_id,"role"),
	CONSTRAINT place_author_link_fk_p FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT place_author_link_fk_a FOREIGN KEY (author_id) REFERENCES public.author(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- placeFeature
CREATE TABLE public.place_feature (
	id serial NOT NULL,
	place_id varchar(20) NOT NULL,
	"type" varchar(50) NOT NULL,
	title varchar(200) NOT NULL,
	geometry varchar(2000) NULL,
	snippet varchar(100) NULL,
	link varchar(200) NULL,
	description varchar(500) NULL,
	"precision" varchar(50) NULL,
	geo geography NULL,
	CONSTRAINT place_feature_pk PRIMARY KEY (id),
	CONSTRAINT place_feature_fk FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- placeLink
CREATE TABLE public.place_link (
	source_id varchar(20) NOT NULL,
	target_id varchar(20) NOT NULL,
	CONSTRAINT place_link_pk PRIMARY KEY (source_id,target_id),
	CONSTRAINT place_link_fk_s FOREIGN KEY (source_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT place_link_fk_t FOREIGN KEY (target_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- placeMeta
CREATE TABLE public.place_meta (
	id serial NOT NULL,
	place_id varchar(20) NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	CONSTRAINT place_meta_pk PRIMARY KEY (id),
	CONSTRAINT place_meta_fk FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- placeReference
CREATE TABLE public.place_reference (
	id serial NOT NULL,
	place_id varchar(20) NOT NULL,
	title varchar(300) NOT NULL,
	type_id integer NOT NULL,
	cit_type_uri_id integer NOT NULL,
	access_uri varchar(1000) NULL,
	alternate_uri varchar(1000) NULL,
	bib_uri varchar(1000) NULL,
	citation varchar(2000) NULL,
	citation_detail varchar(500) NULL,
	other_id varchar(500) NULL,
	CONSTRAINT place_reference_pk PRIMARY KEY (id),
	CONSTRAINT place_reference_fk FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- name
CREATE TABLE public."name" (
	id serial NOT NULL,
	place_id varchar(20) NOT NULL,
	type_id integer NOT NULL,
	uri varchar(200) NOT NULL,
	"language" varchar(50) NOT NULL,
	start_year smallint NOT NULL,
	end_year smallint NOT NULL,
	attested varchar(100) NULL,
	romanized varchar(500) NOT NULL,
	provenance varchar(500) NULL,
	description varchar(500) NULL,
	details text NULL,
	tr_accuracy_id integer NOT NULL,
	tr_completeness_id integer NOT NULL,
	created date NOT NULL,
	modified date NOT NULL,
	certainty_id integer NOT NULL,
	review_state_id integer NOT NULL,
	CONSTRAINT name_pk PRIMARY KEY (id),
	CONSTRAINT name_fk FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- name_attestation
CREATE TABLE public.name_attestation (
	id serial NOT NULL,
	name_id integer NOT NULL,
	period_id integer NOT NULL,
	confidence_id integer NOT NULL,
	"rank" smallint NOT NULL,
	CONSTRAINT name_attestation_pk PRIMARY KEY (id),
	CONSTRAINT name_attestation_fk FOREIGN KEY (name_id) REFERENCES public."name"(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- name_author_link
CREATE TABLE public.name_author_link (
	name_id integer NOT NULL,
	author_id varchar(50) NOT NULL,
	"role" char(1) NOT NULL,
	CONSTRAINT name_author_link_pk PRIMARY KEY (name_id,author_id,"role"),
	CONSTRAINT name_author_link_fk_n FOREIGN KEY (name_id) REFERENCES public."name"(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT name_author_link_fk_a FOREIGN KEY (author_id) REFERENCES public.author(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- name_reference
CREATE TABLE public.name_reference (
	id serial NOT NULL,
	name_id int4 NOT NULL,
	title varchar(300) NOT NULL,
	type_id int4 NOT NULL,
	cit_type_uri_id int4 NOT NULL,
	access_uri varchar(1000) NULL,
	alternate_uri varchar(1000) NULL,
	bib_uri varchar(1000) NULL,
	citation varchar(2000) NULL,
	citation_detail varchar(500) NULL,
	other_id varchar(500) NULL,
	CONSTRAINT name_reference_pk PRIMARY KEY (id)
);
ALTER TABLE public.name_reference ADD CONSTRAINT name_reference_fk FOREIGN KEY (name_id) REFERENCES "name"(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- location
CREATE TABLE public."location" (
	id serial NOT NULL,
	place_id varchar(20) NOT NULL,
	review_state_id integer NOT NULL,
	certainty_id integer NOT NULL,
	accuracy_id integer NOT NULL,
	uri varchar(200) NOT NULL,
	start_year smallint NOT NULL,
	end_year smallint NOT NULL,
	title varchar(200) NOT NULL,
	provenance varchar(500) NULL,
	remains varchar(500) NULL,
	details text NULL,
	accuracy_value double precision NOT NULL,
	description varchar(500) NULL,
	created date NOT NULL,
	modified date NOT NULL,
	geometry varchar(2000) NULL,
	geo geography NULL,
	CONSTRAINT location_pk PRIMARY KEY (id),
	CONSTRAINT location_fk FOREIGN KEY (place_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- location_attestation
CREATE TABLE public.location_attestation (
	id serial NOT NULL,
	location_id integer NOT NULL,
	period_id integer NOT NULL,
	confidence_id integer NOT NULL,
	"rank" smallint NOT NULL,
	CONSTRAINT location_attestation_pk PRIMARY KEY (id),
	CONSTRAINT location_attestation_fk FOREIGN KEY (location_id) REFERENCES public."location"(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- location_author_link
CREATE TABLE public.location_author_link (
	location_id integer NOT NULL,
	author_id varchar(50) NOT NULL,
	"role" char(1) NOT NULL,
	CONSTRAINT location_authorlink_pk PRIMARY KEY (location_id,author_id,"role"),
	CONSTRAINT location_authorlink_fk_l FOREIGN KEY (location_id) REFERENCES public."location"(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT location_authorlink_fk_a FOREIGN KEY (author_id) REFERENCES public.author(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- location_meta
CREATE TABLE public.location_meta (
	id serial NOT NULL,
	location_id integer NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(500) NOT NULL,
	CONSTRAINT location_meta_pk PRIMARY KEY (id),
	CONSTRAINT location_meta_fk FOREIGN KEY (location_id) REFERENCES public."location"(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- location_reference
CREATE TABLE public.location_reference (
	id serial NOT NULL,
	location_id integer NOT NULL,
	title varchar(300) NOT NULL,
	type_id integer NOT NULL,
	cit_type_uri_id integer NOT NULL,
	access_uri varchar(1000) NULL,
	alternate_uri varchar(1000) NULL,
	bib_uri varchar(1000) NULL,
	citation varchar(2000) NULL,
	citation_detail varchar(500) NULL,
	other_id varchar(500) NULL,
	CONSTRAINT location_reference_pk PRIMARY KEY (id),
	CONSTRAINT location_reference_fk FOREIGN KEY (location_id) REFERENCES public."location"(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- connection
CREATE TABLE public."connection" (
	id serial NOT NULL,
	source_id varchar(20) NOT NULL,
	target_id varchar(20) NOT NULL,
	uri varchar(200) NOT NULL,
	type_id integer NOT NULL,
	title varchar(100) NOT NULL,
	description varchar(500) NULL,
	start_year smallint NOT NULL,
	end_year smallint NOT NULL,
	details varchar(5000) NULL,
	provenance varchar(500) NULL,
	certainty_id integer NOT NULL,
	target_uri varchar(200) NOT NULL,
	created date NOT NULL,
	modified date NOT NULL,
	review_state_id integer NOT NULL,
	CONSTRAINT connection_pk PRIMARY KEY (id),
	CONSTRAINT connection_fk_s FOREIGN KEY (source_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT connection_fk_t FOREIGN KEY (target_id) REFERENCES public.place(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- connection_attestation
CREATE TABLE public.connection_attestation (
	id serial NOT NULL,
	connection_id integer NOT NULL,
	period_id integer NOT NULL,
	confidence_id integer NOT NULL,
	"rank" smallint NOT NULL,
	CONSTRAINT connection_attestation_pk PRIMARY KEY (id),
	CONSTRAINT connection_attestation_fk FOREIGN KEY (connection_id) REFERENCES public."connection"(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- connection_author_link
CREATE TABLE public.connection_author_link (
	connection_id integer NOT NULL,
	author_id varchar(50) NOT NULL,
	"role" char(1) NOT NULL,
	CONSTRAINT connection_authorlink_pk PRIMARY KEY (connection_id,author_id,"role"),
	CONSTRAINT connection_authorlink_fk_c FOREIGN KEY (connection_id) REFERENCES public."connection"(id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT connection_authorlink_fk_a FOREIGN KEY (author_id) REFERENCES public.author(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- connection_reference
CREATE TABLE public.connection_reference (
	id serial NOT NULL,
	connection_id integer NOT NULL,
	title varchar(300) NOT NULL,
	type_id integer NOT NULL,
	cit_type_uri_id integer NOT NULL,
	access_uri varchar(1000) NULL,
	alternate_uri varchar(1000) NULL,
	bib_uri varchar(1000) NULL,
	citation varchar(2000) NULL,
	citation_detail varchar(500) NULL,
	other_id varchar(500) NULL,
	CONSTRAINT connection_reference_pk PRIMARY KEY (id),
	CONSTRAINT connection_reference_fk FOREIGN KEY (connection_id) REFERENCES public."connection"(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- AUTH

-- public.app_user definition
CREATE TABLE app_user (
	id text NOT NULL,
	first_name text NULL,
	last_name text NULL,
	user_name varchar(256) NULL,
	normalized_user_name varchar(256) NULL,
	email varchar(256) NULL,
	normalized_email varchar(256) NULL,
	email_confirmed bool NOT NULL,
	password_hash text NULL,
	security_stamp text NULL,
	concurrency_stamp text NULL,
	phone_number text NULL,
	phone_number_confirmed bool NOT NULL,
	two_factor_enabled bool NOT NULL,
	lockout_end timestamptz NULL,
	lockout_enabled bool NOT NULL,
	access_failed_count int4 NOT NULL,
	CONSTRAINT pk_app_user PRIMARY KEY (id)
);
CREATE INDEX "email_index" ON public.app_user USING btree (normalized_email);
CREATE UNIQUE INDEX "user_name_index" ON public.app_user USING btree (normalized_user_name);

-- public.app_role definition
CREATE TABLE app_role (
	id text NOT NULL,
	"name" varchar(256) NULL,
	normalized_name varchar(256) NULL,
	concurrency_stamp text NULL,
	CONSTRAINT pk_app_role PRIMARY KEY (id)
);
CREATE UNIQUE INDEX "role_name_index" ON public.app_role USING btree (normalized_name);

-- public."app_role_claim" definition
CREATE TABLE "app_role_claim" (
	id int4 NOT NULL GENERATED BY DEFAULT AS IDENTITY,
	role_id text NOT NULL,
	claim_type text NULL,
	claim_value text NULL,
	CONSTRAINT "pk_app_role_claim" PRIMARY KEY (id)
);
CREATE INDEX "ix_app_role_claim_role_id" ON public."app_role_claim" USING btree (role_id);
-- public."app_role_claim" foreign keys
ALTER TABLE public."app_role_claim" ADD CONSTRAINT "fk_app_role_claim_aspnetroles_role_id" FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE CASCADE;

-- public."app_user_claim" definition
CREATE TABLE "app_user_claim" (
	id int4 NOT NULL GENERATED BY DEFAULT AS IDENTITY,
	user_id text NOT NULL,
	claim_type text NULL,
	claim_value text NULL,
	CONSTRAINT "pk_app_user_claim" PRIMARY KEY (id)
);
CREATE INDEX "ix_app_user_claim_user_id" ON public."app_user_claim" USING btree (user_id);
-- public."app_user_claim" foreign keys
ALTER TABLE public."app_user_claim" ADD CONSTRAINT "fk_app_user_claim_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;

-- public."app_user_login" definition
CREATE TABLE "app_user_login" (
	login_provider text NOT NULL,
	provider_key text NOT NULL,
	provider_display_name text NULL,
	user_id text NOT NULL,
	CONSTRAINT "pk_app_user_login" PRIMARY KEY (login_provider, provider_key)
);
CREATE INDEX "ix_app_user_login_user_id" ON public."app_user_login" USING btree (user_id);
-- public."app_user_login" foreign keys
ALTER TABLE public."app_user_login" ADD CONSTRAINT "fk_app_user_login_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;

-- public."app_user_role" definition
CREATE TABLE "app_user_role" (
	user_id text NOT NULL,
	role_id text NOT NULL,
	CONSTRAINT "pk_app_user_role" PRIMARY KEY (user_id, role_id)
);
CREATE INDEX "ix_app_user_role_role_id" ON public."app_user_role" USING btree (role_id);
-- public."app_user_role" foreign keys
ALTER TABLE public."app_user_role" ADD CONSTRAINT "fk_app_user_role_aspnetroles_role_id" FOREIGN KEY (role_id) REFERENCES app_role(id) ON DELETE CASCADE;
ALTER TABLE public."app_user_role" ADD CONSTRAINT "fk_app_user_role_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;

-- public."app_user_token" definition
CREATE TABLE "app_user_token" (
	user_id text NOT NULL,
	login_provider text NOT NULL,
	"name" text NOT NULL,
	value text NULL,
	CONSTRAINT "pk_app_user_token" PRIMARY KEY (user_id, login_provider, name)
);
-- public."app_user_token" foreign keys
ALTER TABLE public."app_user_token" ADD CONSTRAINT "fk_app_user_token_aspnetusers_user_id" FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE;
