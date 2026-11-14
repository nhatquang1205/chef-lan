-- Rewrites stored image URLs from one public base to another, in every text column of the courses and
-- customers schemas (thumbnails, gallery, categories, and markdown such as recipes and descriptions).
-- It prints each column it changed and ROLLS BACK unless run with -v commit=1:
--
--   docker exec -i postgres sh -c 'psql -U "$POSTGRES_USER" -d cheflan -v ON_ERROR_STOP=1' < rewrite-image-base-url.sql
--   docker exec -i postgres sh -c 'psql -U "$POSTGRES_USER" -d cheflan -v ON_ERROR_STOP=1 -v commit=1' < rewrite-image-base-url.sql

-- Override with -v old_base=... -v new_base=... (swap them to undo).
\if :{?old_base}
\else
\set old_base 'https://chef-lan.s3.us-east-005.backblazeb2.com/'
\endif
\if :{?new_base}
\else
\set new_base 'https://img.bonbocon.com/'
\endif

BEGIN;

SELECT set_config('rewrite.old', :'old_base', true), set_config('rewrite.new', :'new_base', true);

DO $$
DECLARE
    col record;
    changed bigint;
    old_base text := current_setting('rewrite.old');
    new_base text := current_setting('rewrite.new');
BEGIN
    FOR col IN
        SELECT c.table_schema, c.table_name, c.column_name
        FROM information_schema.columns c
        JOIN information_schema.tables t ON t.table_schema = c.table_schema AND t.table_name = c.table_name
        WHERE c.table_schema IN ('courses', 'customers')
          AND t.table_type = 'BASE TABLE'
          AND c.data_type IN ('text', 'character varying')
    LOOP
        EXECUTE format(
            'UPDATE %I.%I SET %I = replace(%I, $1, $2) WHERE strpos(%I, $1) > 0',
            col.table_schema, col.table_name, col.column_name, col.column_name, col.column_name)
        USING old_base, new_base;

        GET DIAGNOSTICS changed = ROW_COUNT;

        IF changed > 0 THEN
            RAISE NOTICE '%.%.%: % rows', col.table_schema, col.table_name, col.column_name, changed;
        END IF;
    END LOOP;
END $$;

-- Anything still pointing at the old base (should be 0 rows).
SELECT 'courses.course_images' AS "table", count(*) FROM courses.course_images WHERE strpos(url, :'old_base') > 0
UNION ALL
SELECT 'courses.courses', count(*) FROM courses.courses WHERE strpos(coalesce(thumbnail_url, ''), :'old_base') > 0;

\if :{?commit}
COMMIT;
\else
ROLLBACK;
\echo 'Dry run: rolled back. Run again with -v commit=1 to apply.'
\endif
