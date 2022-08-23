PRAGMA journal_mode=WAL;

DROP TABLE IF EXISTS entry;

CREATE TABLE entry (
	  entry_id       INTEGER  NOT NULL  PRIMARY KEY
  , html_content   TEXT     NOT NULL  
  , text_content   TEXT     NOT NULL
  , entry_date     TEXT     NOT NULL
  , modified_date  TEXT     NOT NULL);