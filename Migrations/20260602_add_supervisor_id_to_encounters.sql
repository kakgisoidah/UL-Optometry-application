-- ════════════════════════════════════════════════════════════════════════
--  Migration: add supervisor_id to public.encounters
--  Date:      2026-06-02
--  Reason:    Onsite encounter submission now allows explicit supervisor
--             selection from encounter form or booking assignment flow.
-- ════════════════════════════════════════════════════════════════════════

ALTER TABLE public.encounters
  ADD COLUMN IF NOT EXISTS supervisor_id uuid REFERENCES public.profiles(user_id);

-- Backfill from booking assignments when available
UPDATE public.encounters e
SET supervisor_id = ba.supervisor_id
FROM public.booking_assignments ba
WHERE e.booking_id = ba.booking_id
  AND e.supervisor_id IS NULL
  AND ba.supervisor_id IS NOT NULL;
