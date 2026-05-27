-- ════════════════════════════════════════════════════════════════════════
--  Migration: add supervisor_id to public.encounters
--  Date:      2026-05-27
--  Reason:    The C# Encounter model should declare [Column("supervisor_id")],
--             but this column was missing from the table. Without it:
--               • EncounterService.FillBookingContextAsync cannot store the
--                 supervisor from booking_assignments
--               • Encounter records lack supervisor context
--               • Supervisor-encounter linkage is broken
-- ════════════════════════════════════════════════════════════════════════

-- 1. Add the column (nullable — existing rows keep NULL until backfilled)
ALTER TABLE public.encounters
  ADD COLUMN IF NOT EXISTS supervisor_id uuid REFERENCES public.supervisors(id);

-- 2. Backfill existing encounters from their booking context
--    (only rows that have a booking_id and are still NULL)
UPDATE public.encounters e
SET supervisor_id = ba.supervisor_id
FROM public.bookings b
LEFT JOIN public.booking_assignments ba
       ON ba.booking_id = b.booking_id
WHERE e.booking_id  = b.booking_id
  AND e.supervisor_id IS NULL
  AND ba.supervisor_id IS NOT NULL;

-- 3. Verify — should return 0 rows if backfill succeeded for all linked encounters
-- SELECT id, booking_id, supervisor_id
-- FROM public.encounters
-- WHERE booking_id IS NOT NULL
--   AND supervisor_id IS NULL;
