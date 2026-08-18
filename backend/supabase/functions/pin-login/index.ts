import { serve } from "https://deno.land/std@0.168.0/http/server.ts"
import { createClient } from "https://esm.sh/@supabase/supabase-js@2"
import * as bcrypt from "https://deno.land/x/bcrypt@v0.4.1/mod.ts"
import { create, Header, Payload } from "https://deno.land/x/djwt@v2.8/mod.ts"

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

serve(async (req) => {
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    const { pin } = await req.json()

    if (!pin) {
      return new Response(JSON.stringify({ error: 'PIN is required' }), {
        status: 400,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      })
    }

    const supabaseAdmin = createClient(
      Deno.env.get('SUPABASE_URL') ?? '',
      Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') ?? Deno.env.get('SUPABASE_ANON_KEY') ?? ''
    )

    const { data: employees, error } = await supabaseAdmin
      .from('employees')
      .select('*')
      .eq('active', true)

    if (error) throw error

    let matchedEmployee = null
    for (const employee of employees) {
      const isMatch = await bcrypt.compare(pin, employee.pin_hash)
      if (isMatch) {
        matchedEmployee = employee
        break
      }
    }

    if (!matchedEmployee) {
      return new Response(JSON.stringify({ error: 'Invalid PIN' }), {
        status: 401,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      })
    }

    // Sign a real JWT for Supabase Auth to accept
    const jwtSecret = Deno.env.get('JWT_SECRET')
    if (!jwtSecret) throw new Error('JWT_SECRET not set')

    const encoder = new TextEncoder()
    const keyData = encoder.encode(jwtSecret)
    const cryptoKey = await crypto.subtle.importKey(
      "raw",
      keyData,
      { name: "HMAC", hash: "SHA-256" },
      false,
      ["sign"]
    )

    const header: Header = { alg: "HS256", typ: "JWT" }
    const payload: Payload = {
      role: matchedEmployee.role, // 'admin' or 'worker'
      sub: matchedEmployee.id,
      exp: Math.floor(Date.now() / 1000) + (60 * 60 * 8), // 8 hours
    }

    const token = await create(header, payload, cryptoKey)

    return new Response(
      JSON.stringify({
        user: {
          id: matchedEmployee.id,
          name: matchedEmployee.name,
          role: matchedEmployee.role
        },
        token: token
      }),
      {
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
        status: 200,
      }
    )

  } catch (error) {
    return new Response(JSON.stringify({ error: error.message }), {
      headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      status: 400,
    })
  }
})
