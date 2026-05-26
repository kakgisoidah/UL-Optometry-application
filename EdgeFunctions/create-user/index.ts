// ════════════════════════════════════════════════════════════════════════
//  Supabase Edge Function: create-user
//  Runs server-side with the service role key — the mobile app never
//  needs to hold the key itself.
//
//  Called by UserService.CreateViaEdgeFunctionAsync when
//  USE_EDGE_FUNCTION = true.
// ════════════════════════════════════════════════════════════════════════

import { createClient } from 'https://esm.sh/@supabase/supabase-js@2'

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

Deno.serve(async (req: Request) => {
  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    // ── 1. Verify the caller is a logged-in admin ─────────────────────
    const authHeader = req.headers.get('Authorization')
    if (!authHeader) {
      return json({ error: 'Missing authorization' }, 401)
    }

    // Service-role client — bypasses RLS, used for user creation
    const adminClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!,
      { auth: { autoRefreshToken: false, persistSession: false } }
    )

    // Anon client scoped to the caller's JWT — used to verify identity
    const callerClient = createClient(
      Deno.env.get('SUPABASE_URL')!,
      Deno.env.get('SUPABASE_ANON_KEY')!,
      {
        global: { headers: { Authorization: authHeader } },
        auth:   { autoRefreshToken: false, persistSession: false },
      }
    )

    const { data: { user: caller } } = await callerClient.auth.getUser()
    if (!caller) return json({ error: 'Unauthorized' }, 401)

    const { data: callerProfile } = await adminClient
      .from('profiles')
      .select('role')
      .eq('user_id', caller.id)
      .single()

    if (callerProfile?.role !== 'admin') {
      return json({ error: 'Forbidden: admin only' }, 403)
    }

    // ── 2. Parse request body ─────────────────────────────────────────
    const {
      email, password, fullName, phone, role,
      opNumber, qualification, cubicleIds,
      studentNumber, yearOfStudy,
      idNumber, gender, dateOfBirth,
    } = await req.json()

    // ── 3. Create the auth user ───────────────────────────────────────
    const { data: authData, error: authError } =
      await adminClient.auth.admin.createUser({
        email:         email.toLowerCase().trim(),
        password,
        email_confirm: true,   // skip confirmation email for admin-created accounts
      })

    if (authError || !authData.user) {
      return json({ error: authError?.message ?? 'Failed to create auth user' }, 400)
    }

    const uid = authData.user.id

    // ── 4. Insert public.profiles row ─────────────────────────────────
    const { error: profileError } = await adminClient
      .from('profiles')
      .insert({
        user_id:             uid,
        full_name:           fullName.trim(),
        email:               email.toLowerCase().trim(),
        phone:               phone?.trim() ?? '',
        role,
        must_change_password: true,
        is_active:           true,
      })

    if (profileError) {
      await adminClient.auth.admin.deleteUser(uid)   // rollback
      return json({ error: profileError.message }, 500)
    }

    // ── 5. Insert role-specific row ───────────────────────────────────
    if (role === 'supervisor') {
      const { data: supData, error: supError } = await adminClient
        .from('supervisors')
        .insert({
          user_id:       uid,
          op_number:     opNumber?.trim()      ?? '',
          qualification: qualification?.trim() ?? '',
        })
        .select('id')
        .single()

      if (supError) {
        await adminClient.auth.admin.deleteUser(uid)
        return json({ error: supError.message }, 500)
      }

      if (Array.isArray(cubicleIds) && cubicleIds.length > 0 && supData) {
        const cubRows = cubicleIds.map((cid: number) => ({
          supervisor_id: supData.id,
          cubicle_id:    cid,
        }))
        await adminClient.from('supervisor_cubicles').insert(cubRows)
      }

    } else if (role === 'student') {
      await adminClient.from('students').insert({
        user_id:        uid,
        student_number: studentNumber?.trim() ?? '',
        year_of_study:  yearOfStudy ?? 1,
      })

    } else if (role === 'patient') {
      await adminClient.from('patients').insert({
        user_id:       uid,
        id_number:     idNumber   ?? null,
        date_of_birth: dateOfBirth ?? null,
        gender:        gender      ?? null,
      })
    }

    // ── 6. Return the new user's ID ───────────────────────────────────
    return json({ userId: uid }, 200)

  } catch (err) {
    return json({ error: String(err) }, 500)
  }
})

function json(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, 'Content-Type': 'application/json' },
  })
}
