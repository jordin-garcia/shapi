import os
import re

configs_dir = 'src/Shapi.Infraestructura/Persistencia/Configuraciones'

enums = {
    'TipoOrganizacion': {'Plataforma': 'plataforma', 'Proveedor': 'proveedor'},
    'EstadoAdmin': {'Activa': 'activa', 'Suspendida': 'suspendida'},
    'Rol': {'Administrador': 'administrador', 'Soporte': 'soporte', 'Propietario': 'propietario', 'Editor': 'editor', 'Lector': 'lector'},
    'EstadoCuenta': {'Activo': 'activo', 'Desactivado': 'desactivado'},
    'TipoToken': {'VerificacionCorreo': 'verificacion_correo', 'Recuperacion': 'recuperacion', 'InvitacionMiembro': 'invitacion_miembro', 'InvitacionConsumidor': 'invitacion_consumidor', 'DefinirContrasena': 'definir_contrasena'},
    'AmbitoSesion': {'Personal': 'personal', 'Consumidor': 'consumidor'},
    'EstadoApi': {'Borrador': 'borrador', 'Publicada': 'publicada', 'Despublicada': 'despublicada'},
    'MetodoHttp': {'Get': 'GET', 'Post': 'POST', 'Put': 'PUT', 'Patch': 'PATCH', 'Delete': 'DELETE', 'Head': 'HEAD', 'Options': 'OPTIONS'},
    'EstadoDominio': {'Pendiente': 'pendiente', 'Verificado': 'verificado', 'Fallido': 'fallido'},
    'EstadoSuscripcion': {'Activa': 'activa', 'EnGracia': 'en_gracia', 'Suspendida': 'suspendida', 'Finalizada': 'finalizada'},
    'TipoClave': {'Produccion': 'produccion', 'Pruebas': 'pruebas'},
    'EstadoClave': {'Activa': 'activa', 'Rotada': 'rotada', 'Revocada': 'revocada'},
    'MarcaTarjeta': {'Visa': 'Visa', 'Mastercard': 'Mastercard', 'AmericanExpress': 'American Express'},
    'ConceptoPago': {'Contratacion': 'contratacion', 'Renovacion': 'renovacion', 'CambioPlan': 'cambio_plan', 'Reactivacion': 'reactivacion'},
    'EstadoPago': {'Autorizado': 'autorizado', 'Rechazado': 'rechazado', 'Revertido': 'revertido'},
    'EntornoConsumo': {'Produccion': 'produccion', 'Pruebas': 'pruebas'},
    'EstadoCaso': {'Abierto': 'abierto', 'Cerrado': 'cerrado'},
    'ActorTipo': {'Usuario': 'usuario', 'Consumidor': 'consumidor', 'Sistema': 'sistema'},
    'EstadoCorreo': {'Pendiente': 'pendiente', 'Enviado': 'enviado', 'Fallido': 'fallido'},
    'LogoTipo': {'Png': 'image/png', 'Svg': 'image/svg+xml'},
    'EspecificacionFormato': {'Json': 'json', 'Yaml': 'yaml'},
    'RevocadaPor': {'Consumidor': 'consumidor', 'Proveedor': 'proveedor'}
}

def get_converter_code(enum_name):
    v_expr = ""
    s_expr = ""
    for k, v in enums[enum_name].items():
        v_expr += f"v == {enum_name}.{k} ? \"{v}\" : "
        s_expr += f"s == \"{v}\" ? {enum_name}.{k} : "
    
    # default case
    # we need a fallback that compiles
    fallback_v = f"\"{list(enums[enum_name].values())[0]}\""
    fallback_s = f"{enum_name}.{list(enums[enum_name].keys())[0]}"
    
    v_expr += fallback_v
    s_expr += fallback_s
    
    return f"HasConversion(v => {v_expr}, s => {s_expr})"

for filename in os.listdir(configs_dir):
    if not filename.endswith('.cs'): continue
    path = os.path.join(configs_dir, filename)
    with open(path, 'r') as f:
        content = f.read()

    # We need to replace the `HasConversion(...)` blocks that I just generated with the new one.
    # The previous block was:
    # HasConversion(
    #         v => v switch {
    #             ...
    #         },
    #         s => s switch {
    #             ...
    #         })
    
    # Let's use regex to find all `HasConversion(\s*v => v switch \{.*?\s*\}\s*,\s*s => s switch \{.*?\s*\}\s*\)`
    
    def replacer(match):
        enum_name = None
        # Try to infer enum_name from the match contents
        text = match.group(0)
        for en in enums.keys():
            if f"{en}." in text:
                enum_name = en
                break
        if enum_name:
            return get_converter_code(enum_name)
        return text

    new_content = re.sub(r'HasConversion\(\s*v => v switch \{.*?\},\s*s => s switch \{.*?\}\)', replacer, content, flags=re.DOTALL)
    
    with open(path, 'w') as f:
        f.write(new_content)

