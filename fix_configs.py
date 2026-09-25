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
    cases_v = []
    cases_s = []
    for k, v in enums[enum_name].items():
        cases_v.append(f"{enum_name}.{k} => \"{v}\",")
        cases_s.append(f"\"{v}\" => {enum_name}.{k},")
    cases_v.append("_ => throw new ArgumentOutOfRangeException()")
    cases_s.append("_ => throw new ArgumentOutOfRangeException()")
    cv = "\n                ".join(cases_v)
    cs = "\n                ".join(cases_s)
    return f"""HasConversion(
            v => v switch {{
                {cv}
            }},
            s => s switch {{
                {cs}
            }})"""

for filename in os.listdir(configs_dir):
    if not filename.endswith('.cs'): continue
    path = os.path.join(configs_dir, filename)
    with open(path, 'r') as f:
        content = f.read()

    # 1. Replace HasConversion
    def replacer(match):
        prop_name = match.group(1)
        enum_name = None
        if filename == 'OrganizacionConfiguracion.cs':
            if prop_name == 'Tipo': enum_name = 'TipoOrganizacion'
            if prop_name == 'EstadoAdmin': enum_name = 'EstadoAdmin' # wait, the property is actually Estado? or EstadoAdmin? Let's check
        
        # We need to map correctly based on property names
        mapping = {
            'OrganizacionConfiguracion.cs': {'Tipo': 'TipoOrganizacion', 'EstadoAdmin': 'EstadoAdmin', 'Estado': 'EstadoAdmin'},
            'CasoConfiguracion.cs': {'Estado': 'EstadoCaso'},
            'RutaConfiguracion.cs': {'Metodo': 'MetodoHttp'},
            'ConsumidorConfiguracion.cs': {'Estado': 'EstadoCuenta'},
            'UsuarioConfiguracion.cs': {'Estado': 'EstadoCuenta'},
            'CorreoSalienteConfiguracion.cs': {'Estado': 'EstadoCorreo'},
            'PagoConfiguracion.cs': {'Concepto': 'ConceptoPago', 'Estado': 'EstadoPago'},
            'MembresiaConfiguracion.cs': {'Rol': 'Rol'},
            'TokenConfiguracion.cs': {'Tipo': 'TipoToken'},
            'SesionConfiguracion.cs': {'Ambito': 'AmbitoSesion'},
            'DominioPropioConfiguracion.cs': {'Estado': 'EstadoDominio'},
            'SuscripcionApiConfiguracion.cs': {'Estado': 'EstadoSuscripcion'},
            'SuscripcionPlataformaConfiguracion.cs': {'Estado': 'EstadoSuscripcion'},
            'ConsumoDiarioConfiguracion.cs': {'Entorno': 'EntornoConsumo'},
            'MedioPagoConfiguracion.cs': {'Marca': 'MarcaTarjeta'},
            'EntradaBitacoraConfiguracion.cs': {'ActorTipo': 'ActorTipo'},
            'ClaveConfiguracion.cs': {'Tipo': 'TipoClave', 'Estado': 'EstadoClave', 'RevocadaPor': 'RevocadaPor'},
            'ApiConfiguracion.cs': {'Estado': 'EstadoApi', 'EspecificacionFormato': 'EspecificacionFormato', 'PortalLogoTipo': 'LogoTipo'}
        }
        
        enum_name = mapping.get(filename, {}).get(prop_name)
        if enum_name:
            return match.group(0).replace('.HasConversion<string>()', '.' + get_converter_code(enum_name))
        
        return match.group(0)

    content = re.sub(r'Property\(x => x\.([A-Za-z0-9_]+)\)(.*?)\.HasConversion<string>\(\)', replacer, content, flags=re.DOTALL)
    
    # 2. Fix CHECK constraint values
    # e.g., IN ('Activa','Suspendida') -> IN ('activa','suspendida')
    # and AmericanExpress -> American Express
    
    # Let's just do text replacements for the specific exact strings
    replacements = [
        ("IN ('Plataforma','Proveedor')", "IN ('plataforma','proveedor')"),
        ("IN ('Activa','Suspendida')", "IN ('activa','suspendida')"),
        ("IN ('Administrador','Soporte','Propietario','Editor','Lector')", "IN ('administrador','soporte','propietario','editor','lector')"),
        ("IN ('Activo','Desactivado')", "IN ('activo','desactivado')"),
        ("IN ('VerificacionCorreo','Recuperacion','InvitacionMiembro','InvitacionConsumidor','DefinirContrasena')", "IN ('verificacion_correo','recuperacion','invitacion_miembro','invitacion_consumidor','definir_contrasena')"),
        ("IN ('Personal','Consumidor')", "IN ('personal','consumidor')"),
        ("IN ('Borrador','Publicada','Despublicada')", "IN ('borrador','publicada','despublicada')"),
        ("IN ('Get','Post','Put','Patch','Delete','Head','Options')", "IN ('GET','POST','PUT','PATCH','DELETE','HEAD','OPTIONS')"),
        ("IN ('Pendiente','Verificado','Fallido')", "IN ('pendiente','verificado','fallido')"),
        ("IN ('Activa','EnGracia','Suspendida','Finalizada')", "IN ('activa','en_gracia','suspendida','finalizada')"),
        ("IN ('Produccion','Pruebas')", "IN ('produccion','pruebas')"),
        ("IN ('Activa','Rotada','Revocada')", "IN ('activa','rotada','revocada')"),
        ("IN ('Visa','Mastercard','AmericanExpress')", "IN ('Visa','Mastercard','American Express')"),
        ("IN ('Contratacion','Renovacion','CambioPlan','Reactivacion')", "IN ('contratacion','renovacion','cambio_plan','reactivacion')"),
        ("IN ('Autorizado','Rechazado','Revertido')", "IN ('autorizado','rechazado','revertido')"),
        ("IN ('Abierto','Cerrado')", "IN ('abierto','cerrado')"),
        ("IN ('Usuario','Consumidor','Sistema')", "IN ('usuario','consumidor','sistema')"),
        ("IN ('Pendiente','Enviado','Fallido')", "IN ('pendiente','enviado','fallido')"),
        ("IN ('Png','Svg')", "IN ('image/png','image/svg+xml')")
    ]
    for old, new in replacements:
        content = content.replace(old, new)
        
    # 3. Fix partial indices
    # "tipo = 'Plataforma'" -> "tipo = 'plataforma'"
    # "rol = 'Propietario'" -> "rol = 'propietario'"
    # "estado <> 'Finalizada'" -> "estado <> 'finalizada'"
    # "estado = 'Activa'" -> "estado = 'activa'"
    # "es_prueba = true" might already be true, let's make sure
    
    content = content.replace('tipo = \'Plataforma\'', "tipo = 'plataforma'")
    content = content.replace('rol = \'Propietario\'', "rol = 'propietario'")
    content = content.replace('estado <> \'Finalizada\'', "estado <> 'finalizada'")
    content = content.replace('estado = \'Activa\'', "estado = 'activa'")
    content = content.replace('es_prueba = True', "es_prueba = true")

    with open(path, 'w') as f:
        f.write(content)

