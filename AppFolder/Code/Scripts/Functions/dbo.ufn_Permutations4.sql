
IF OBJECT_ID('dbo.ufn_Permutations4', 'IF') IS NOT NULL
    DROP FUNCTION dbo.ufn_Permutations4;
GO

-- Generates all 4-character permutations from A..Z.
-- @AllowRepeats = 1  -> repetitions allowed (26^4 rows)
-- @AllowRepeats = 0  -> no repetitions (P(26,4) rows)
-- @Lowercase    = 1  -> output a..z instead of A..Z
CREATE FUNCTION dbo.ufn_Permutations4
(
    @AllowRepeats BIT = 0,
    @Lowercase    BIT = 0
)
RETURNS TABLE
AS
RETURN
WITH N AS
(
    -- Build 26 numbers: 0..25 using catalog view (safe for SQL Server 2008)
    SELECT TOP (26) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
    FROM sys.all_objects
),
Alpha AS
(
    -- Map each number to a letter
    SELECT
        n,
        ch = CHAR(CASE WHEN @Lowercase = 1 THEN 97 ELSE 65 END + n)  -- 97='a', 65='A'
    FROM N
)
SELECT
    SeqNo = ROW_NUMBER() OVER (ORDER BY a1.n, a2.n, a3.n, a4.n),
    Code  = CAST(a1.ch + a2.ch + a3.ch + a4.ch AS CHAR(4))
FROM Alpha AS a1
CROSS JOIN Alpha AS a2
CROSS JOIN Alpha AS a3
CROSS JOIN Alpha AS a4
WHERE
    -- If repeats are not allowed, enforce all four positions are distinct
    (@AllowRepeats = 1)
    OR (a1.n <> a2.n AND a1.n <> a3.n AND a1.n <> a4.n
                     AND a2.n <> a3.n AND a2.n <> a4.n
                                      AND a3.n <> a4.n);
GO
