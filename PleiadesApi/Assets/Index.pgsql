-- eix_token
CREATE TABLE public.eix_token (
	id serial NOT NULL,
	value varchar(100) NOT NULL,
	"language" varchar(5) NULL,
	CONSTRAINT token_pk PRIMARY KEY (id)
);
CREATE INDEX eix_token_value_idx ON public.eix_token (value);

-- eix_occurrence
CREATE TABLE public.eix_occurrence (
	id serial NOT NULL,
	token_id int4 NOT NULL,
	field bpchar(5) NOT NULL,
	target_id varchar(20) NOT NULL,
	"rank" int4 NULL,
	year_min int2 NULL,
	year_max int2 NULL,
	CONSTRAINT occurrence_pk PRIMARY KEY (id)
);
CREATE INDEX eix_occurrence_field_idx ON public.eix_occurrence USING btree (field);
ALTER TABLE public.eix_occurrence ADD CONSTRAINT eix_occurrence_fk FOREIGN KEY (tokenid) REFERENCES eix_token(id) ON DELETE CASCADE ON UPDATE CASCADE;
