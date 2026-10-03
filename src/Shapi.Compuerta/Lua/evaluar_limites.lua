-- evaluar_limites.lua (08 §3, ADR-21): reserva atómica de los límites por minuto y de las cuotas de una petición.
-- Redis ejecuta el script completo sin intercalar otros comandos, así que la cuota nunca se excede.
--
-- ARGV[1] limite_minuto   límite por minuto del plan (o el fijo de la clave de pruebas)
-- ARGV[2] limite_ruta     límite por minuto de la ruta; 0 si no tiene
-- ARGV[3] peso            llamadas que descuenta la petición
-- ARGV[4] limite_cuota    cuota del ciclo (o el límite diario de la clave de pruebas)
-- ARGV[5] expira_cuota    EXPIREAT del contador de la cuota (segundos Unix)
-- ARGV[6] limite_plat     cuota de plataforma del proveedor; 0 si no tiene
-- ARGV[7] expira_plat     EXPIREAT del contador de la cuota de plataforma (segundos Unix)
--
-- KEYS, en este orden y solo las que aplican: rl_minuto, rl_ruta (si limite_ruta > 0), cuota,
-- cuota_plat (si limite_plat > 0).
--
-- Devuelve {codigo, n_minuto, n_ruta, n_cuota, n_plat}: los contadores después de la reserva o de revertirla.
-- codigo: 0 = se reservó, 1 = límite por minuto del plan, 2 = límite por minuto de la ruta, 3 = cuota,
-- 4 = cuota de plataforma.

local limite_minuto = tonumber(ARGV[1])
local limite_ruta = tonumber(ARGV[2])
local peso = tonumber(ARGV[3])
local limite_cuota = tonumber(ARGV[4])
local expira_cuota = tonumber(ARGV[5])
local limite_plat = tonumber(ARGV[6])
local expira_plat = tonumber(ARGV[7])

local siguiente = 1
local function llave(aplica)
  if not aplica then
    return nil
  end
  local nombre = KEYS[siguiente]
  siguiente = siguiente + 1
  return nombre
end

local rl_minuto = llave(true)
local rl_ruta = llave(limite_ruta > 0)
local cuota = llave(true)
local cuota_plat = llave(limite_plat > 0)

local function leer(nombre)
  if not nombre then
    return 0
  end
  return tonumber(redis.call('GET', nombre) or '0')
end

local function resultado(codigo)
  return {codigo, leer(rl_minuto), leer(rl_ruta), leer(cuota), leer(cuota_plat)}
end

-- 1. Límite por minuto del plan. La ventana es el minuto de la llave; el TTL solo la limpia.
local n = redis.call('INCR', rl_minuto)
if n == 1 then
  redis.call('EXPIRE', rl_minuto, 120)
end
if n > limite_minuto then
  redis.call('DECR', rl_minuto)
  return resultado(1)
end

-- 2. Límite por minuto de la ruta.
if rl_ruta then
  n = redis.call('INCR', rl_ruta)
  if n == 1 then
    redis.call('EXPIRE', rl_ruta, 120)
  end
  if n > limite_ruta then
    redis.call('DECR', rl_ruta)
    redis.call('DECR', rl_minuto)
    return resultado(2)
  end
end

-- 3. Cuota del consumidor: descuenta el peso de la ruta.
n = redis.call('INCRBY', cuota, peso)
redis.call('EXPIREAT', cuota, expira_cuota)
if n > limite_cuota then
  redis.call('DECRBY', cuota, peso)
  if rl_ruta then
    redis.call('DECR', rl_ruta)
  end
  redis.call('DECR', rl_minuto)
  return resultado(3)
end

-- 4. Cuota de plataforma del proveedor: cuenta peticiones.
if cuota_plat then
  n = redis.call('INCR', cuota_plat)
  redis.call('EXPIREAT', cuota_plat, expira_plat)
  if n > limite_plat then
    redis.call('DECR', cuota_plat)
    redis.call('DECRBY', cuota, peso)
    if rl_ruta then
      redis.call('DECR', rl_ruta)
    end
    redis.call('DECR', rl_minuto)
    return resultado(4)
  end
end

return resultado(0)
