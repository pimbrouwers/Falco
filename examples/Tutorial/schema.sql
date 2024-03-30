DROP TABLE IF EXISTS entry;

CREATE TABLE entry (
	  entry_id       TEXT     NOT NULL  PRIMARY KEY
  , html_content   TEXT     NOT NULL
  , text_content   TEXT     NOT NULL
  , modified_date  TEXT     NOT NULL);