# Lineamientos del Proyecto Final de Ingeniería de Software 2026

> Transcripción fiel del documento oficial del curso, *LINEAMIENTOS PROYECTO ING SOFT 2026*. Es la fuente de las reglas que el proyecto debe cumplir. Solo se corrigieron errores de tipeo evidentes, anotados con *[sic]*. Cómo cumple Shapi cada punto está en [specs/03-requisitos.md §3](specs/03-requisitos.md#3-trazabilidad-con-los-lineamientos) y [specs/01-vision-y-alcance.md §2](specs/01-vision-y-alcance.md#2-categoria-del-enunciado).

**Universidad Rafael Landívar** — Identidad Jesuita en Guatemala
Facultad de Ingeniería · Ingeniería en Informática y Sistemas · Curso: Ingeniería de Software I

---

## Proyecto Final de Ingeniería de Software 2026

Este proyecto integrará los conocimientos de análisis y diseño, arquitectura, desarrollo, despliegue y operación de servicios en la nube.

## Desarrollo de una Plataforma como Servicio (PaaS)

### 1. Descripción General

Los estudiantes deberán desarrollar una plataforma de tipo **Platform as a Service (PaaS)** que permita a usuarios registrarse, contratar servicios tecnológicos y administrarlos desde una interfaz web.

La plataforma deberá funcionar como un proveedor de servicios en la nube y ofrecer al menos una de las siguientes categorías:

- Servicio de Bases de Datos (DBaaS)
- Servicio de Almacenamiento (Storage as a Service)
- Servicio de Servidores Web (Web Hosting Service)

**Cada equipo deberá seleccionar únicamente una categoría como producto principal.**

### 2. Objetivo del Proyecto

Diseñar, desarrollar, documentar e implementar una solución PaaS aplicando las buenas prácticas de Ingeniería de Software, arquitectura de sistemas, gestión de proyectos y despliegue en la nube.

Los estudiantes deberán demostrar competencias en:

- Levantamiento de requisitos.
- Diseño de arquitectura.
- Desarrollo Full Stack.
- Administración de usuarios.
- Gestión de pagos.
- Despliegue en ambientes productivos.
- Trabajo colaborativo.

### 3. Conformación de Equipos

- Los equipos estarán integrados por **4 estudiantes**.
- La conformación de equipos será realizada exclusivamente por el docente.
- Los integrantes serán asignados de forma aleatoria.
- No se permitirán cambios de equipo salvo autorización expresa del docente.

### 4. Características Obligatorias del Sistema

#### 4.1 Gestión de Usuarios

La plataforma deberá incluir:

**Registro**
- Registro de nuevos usuarios.
- Validación de correo electrónico.
- Recuperación de contraseña.

**Inicio de Sesión**
- Autenticación segura.
- Manejo de sesiones.

**Roles**

Como mínimo:
- Administrador
- Cliente

Opcionalmente:
- Soporte técnico
- Operador de infraestructura

#### 4.2 Gestión de Suscripciones y Pagos

La plataforma deberá simular o implementar un sistema de monetización.

Cada servicio ofertado deberá tener:
- Nombre.
- Descripción.
- Precio.
- Vigencia.

Funciones mínimas:
- Visualización de planes.
- Contratación de servicios.
- Historial de pagos.
- Renovación de servicios.

Se podrá utilizar:
- Simulación propia de pagos.

No se exige el uso de dinero real.

#### 4.3 Catálogo de Servicios

**Opción A: Base de Datos como Servicio (DBaaS)**

Ejemplos:
- MySQL
- PostgreSQL
- MongoDB

El usuario podrá:
- Crear instancias.
- Configurar parámetros básicos.
- Consultar estado del servicio.

**Opción B: Almacenamiento como Servicio**

Ejemplos:
- Gestión de archivos.
- Almacenamiento de documentos.
- Espacios virtuales de almacenamiento.

El usuario podrá:
- Cargar archivos.
- Descargar archivos.
- Gestionar carpetas.
- Monitorear espacio disponible.

**Opción C: Servidores Web como Servicio**

Ejemplos:
- Hosting de aplicaciones.
- Creación de sitios web.
- Gestión de dominios simulados.

El usuario podrá:
- Crear entornos web.
- Gestionar configuraciones.
- Monitorear consumo de recursos.

### 5. Requisitos Técnicos

**Backend.** Podrán utilizar:
- Java Spring Boot
- .NET
- Node.js
- Python Django
- Python FastAPI

**Frontend.** Podrán utilizar:
- Angular
- React
- Vue
- Blazor
- HTML/CSS/JavaScript

**Base de Datos**
- PostgreSQL
- MySQL
- SQL Server
- MongoDB

### 6. Requisitos de Arquitectura

La solución deberá incluir:

**LA ARQUITECTURA QUE CADA GRUPO DECIDA**
**(Esta arquitectura debe estar analizada y debidamente defendida)**

### 7. Documentación Obligatoria

**Documento de Requisitos.** Debe incluir:
- Introducción.
- Alcance.
- Requisitos funcionales.
- Requisitos no funcionales.
- Casos de uso.

**Documento de Diseño.** Debe incluir:
- Arquitectura general.
- Diagramas UML.
- Modelo entidad-relación.
- Diseño de base de datos.

**Manual Técnico.** Debe incluir:
- Tecnologías utilizadas.
- Instalación.
- Configuración.
- Despliegue.

**Manual de Usuario.** Debe incluir:
- Registro.
- Inicio de sesión.
- Contratación de servicios.
- Administración de recursos.

### 8. Entregables

**Entrega Final.** Cada equipo deberá presentar:

1. **Código Fuente.** Repositorio GitHub con acceso para el docente.
2. **Documentación.** En formato PDF.
3. **Debe mostrar: Sistema Funcionando.** *[sic: "istema Funcionando" en el original]*

### 9. Cronograma de entregas

| Actividad | Fecha Límite |
|---|---|
| Coordinación de equipos. Entrega de propuesta, alcance y cronograma propuesto | 28 agosto 2026 |
| Entrega de requisitos y diseño preliminar | 11 septiembre 2026 |
| Avance funcional 1 (30%) | 25 septiembre 2026 |
| Avance funcional 2 (50%) | 9 octubre 2026 |
| Avance funcional 3 (80%) | 23 octubre 2026 |
| Entrega final y exposiciones | Primera semana de noviembre de 2026 |

### Desafío Profesional

El proyecto deberá ser concebido como si el equipo estuviera creando una startup proveedora de servicios en la nube. Se evaluará no solamente el funcionamiento técnico, sino también la experiencia de usuario, la calidad de la arquitectura y la capacidad del equipo para defender sus decisiones de diseño durante la exposición final.
