# 📚 Épica 4 — Índice de Documentación y Navegación

**Última actualización:** 2026-04-16  
**Ramas:** 4 creadas ✅  
**Status:** 🚀 Listo para Claude Code

---

## 📋 Documentos Clave (Lee en este orden)

### 1. **EPICA-4-Resumen-Ejecutivo.md** ⭐ EMPIEZA AQUÍ

**Duración:** 5 min  
**Para:** Quién acaba de llegar y necesita saber QUÉ hacer AHORA.

**Contiene:**

- ✅ Estado actual + 3 bloqueadores críticos
- 📋 Desglose de KAN-13..16 (qué falta, bloqueadores, tiempo)
- 🔧 Comandos rápidos
- 📂 Estructura de carpetas a crear
- ⚡ Plan de trabajo día-a-día
- ✅ Checklist pre-start

**Acción:** Lee esto primero. Si entiendes, sigue a documento #2.

---

### 2. **EPICA-4-Plan-Tecnico-TasaRetencion.md** 📖 REFERENCIA TÉCNICA

**Duración:** 30 min (skim) o 2h (completo)  
**Para:** Entender cada KAN en profundidad + especificaciones exactas.

**Secciones:**

- 🎯 Descripción general épica
- 📋 KAN-13 (config) — 10 páginas completas
  - Alcance, user stories, RFs, entidades, use cases, DTOs, validadores, BD, UI, permisos, dependencias
- 📋 KAN-14 (simulación) — 12 páginas
  - Lógica motor ciclo-a-ciclo, transacciones, inflación
- 📋 KAN-15 (indicadores) — 4 páginas
- 📋 KAN-16 (trazabilidad) — 3 páginas
- 🏗️ Arquitectura + patrones (código ejemplo)
- 🔌 Dependencias externas
- 🛑 Bloqueadores conocidos
- 📖 Instrucciones para Claude Code (flujo rama-a-rama)

**Acción:** Úsalo como especificación durante desarrollo. Consulta sección por KAN.

---

### 3. **EPICA-4-Bloqueadores-Limitantes.md** 🚨 ANÁLISIS RIESGOS

**Duración:** 10 min  
**Para:** Ver qué puede salir mal + plan de mitigación.

**Contiene:**

- 🟢 LO QUE SÍ ESTÁ LISTO (9 cosas)
- 🔴 BLOQUEADORES CRÍTICOS (3 bloqueadores, 1 hora total resolver)
- 🟡 BLOQUEADORES MEDIANO IMPACTO (3 cosas, no bloquean pero demoran)
- 🟠 LIMITANTES POTENCIALES (3 cosas, no bloquean pero requieren atención)
- 📊 Matriz de bloqueadores (qué/severidad/tiempo/acción)
- ✅ Plan de desbloqueo (Fase 0–4)
- 🎯 Recomendación final

**Acción:** Revisar si KAN-14 se atasca. Resolver bloqueadores 1–3 primero.

---

### 4. **Este documento: Índice de Navegación** 🗺️

**Duración:** 3 min  
**Para:** Saber dónde está todo.

---

## 🌳 Estructura de Proyecto (Post-Épica 4)

```
d:\Tesis\SistemaAranceles\
├── 📁 docs/
│   ├── EPICA-4-Resumen-Ejecutivo.md ⭐ Empieza aquí (resumen 1 página)
│   ├── EPICA-4-Plan-Tecnico-TasaRetencion.md 📖 Especificación detallada
│   ├── EPICA-4-Bloqueadores-Limitantes.md 🚨 Riesgos + mitigación
│   ├── EPICA-4-Indice-Navegacion.md 🗺️ (este archivo)
│   ├── database/
│   │   ├── KAN-03-modelo-db.md (tabla 29, índices, campos base)
│   │   └── KAN-03-data-dictionary.md
│   ├── contexto.md (contexto maestro proyecto)
│   ├── Diagramas.md (arquitectura diagramas)
│   └── EPICA-2-Informe-KAN06-KAN09.md
├── 📁 src/
│   ├── Domain/Entities/
│   │   ├── ConfiguracionRetencion.cs ✅ existe
│   │   ├── CriterioReferenciaRetencion.cs ✅ existe
│   │   ├── SimulacionRetencion.cs ❌ CREAR KAN-14
│   │   └── DetalleSimulacionRetencion.cs ❌ CREAR KAN-14
│   ├── Application/UseCases/TasaRetencion/
│   │   ├── KAN-13/ (7 use cases)
│   │   ├── KAN-14/ (6 use cases + motor)
│   │   ├── KAN-15/ (2 use cases)
│   │   └── KAN-16/ (3 use cases)
│   ├── Application/DTOs/TasaRetencion/ (15+ DTOs)
│   ├── Infrastructure/Persistence/
│   │   ├── RepositorioConfiguracionRetencion.cs ❌ CREAR KAN-13
│   │   ├── RepositorioCriterioReferenciaRetencion.cs ❌ CREAR KAN-13
│   │   ├── RepositorioSimulacionRetencion.cs ❌ CREAR KAN-14
│   │   └── RepositorioDetalleSimulacionRetencion.cs ❌ CREAR KAN-14
│   └── Presentation/
│       ├── ViewModels/ConfiguracionRetencionViewModel.cs ❌ CREAR KAN-13
│       ├── ViewModels/IndicadoresRetencionViewModel.cs ❌ CREAR KAN-15
│       ├── Views/ConfiguracionRetencionView.xaml ❌ CREAR KAN-13
│       └── Views/IndicadoresRetencionView.xaml ❌ CREAR KAN-15
├── 📁 sql/
│   ├── KAN-14-Apply.sql (migración, creado por `dotnet ef migrations`)
│   └── KAN-14-Rollback.sql ⏳ POST-RELEASE
├── SistemaAranceles.sln
└── README.md
```

---

## 🔄 Flujo de Ramas (GitFlow)

```
main (production)
  ↑
  │ (release)
  │
develop
  ↑
  ├── feature/KAN-13-config-carrera-cohorte (4 SP, 3 días)
  ├── feature/KAN-14-simulacion-cohorte (5 SP, 4 días, 3 bloqueadores)
  ├── feature/KAN-15-indicadores-retencion (3 SP, 2 días, paralelo OK)
  └── feature/KAN-16-edicion-trazabilidad (2 SP, 1 día)
```

**Merge order:** KAN-13 → develop → KAN-14 → develop → ... → KAN-16 → develop → main.

---

## 🎯 Roadmap Ejecución (8–10 días)

| Fase | KAN | Objetivo                                          | SP  | Días | Bloqueadores | Status   |
| ---- | --- | ------------------------------------------------- | --- | ---- | ------------ | -------- |
| 1    | 13  | Config carrera/cohorte                            | 4   | 3    | Ninguno      | 🟢 Ready |
| 2a   | 14  | **Desbloquear**: Entidades + Repos + Migración BD | —   | 0.5  | 3 crít.      | 🔴 TODO  |
| 2b   | 14  | Motor simulación (ciclos)                         | 5   | 4    | Depende 2a   | 🟢 Ready |
| 3    | 15  | Indicadores + Gráficos (paralelo OK)              | 3   | 2    | OxyPlot      | 🟢 Ready |
| 4    | 16  | Trazabilidad auditoría                            | 2   | 1    | Ninguno      | 🟢 Ready |
| 5    | —   | Merge develop → main + tag                        | —   | 1    | QA OK        | 🟢 Ready |

**Total:** 14 SP → ~8–10 días 1 dev, 5–7 días 2 devs.

---

## 🔍 ¿Dónde Buscar Qué?

### **"Necesito crear un Use Case"**

→ Ver EPICA-4-Plan-Tecnico-TasaRetencion.md → sección "KAN-XX" → "Use Cases Necesarios"  
→ Ejemplo patrón: src/Application/UseCases/Inflacion/ProyectarInflacionUseCase.cs

### **"Necesito crear una entidad de dominio"**

→ Ver EPICA-4-Plan-Tecnico-TasaRetencion.md → sección "Entidades de Dominio + BD"  
→ Ejemplo patrón: src/Domain/Entities/InflacionAnual.cs

### **"Necesito crear un DTO"**

→ Ver EPICA-4-Plan-Tecnico-TasaRetencion.md → sección KAN-XX → "DTOs Necesarios"  
→ Ejemplo patrón: src/Application/DTOs/Inflacion/\*Dto.cs

### **"Necesito crear un repositorio"**

→ Ver EPICA-4-Plan-Tecnico-TasaRetencion.md → sección "Arquitectura y Patrones"  
→ Ejemplo patrón: src/Infrastructure/Persistence/RepositorioInflacionAnual.cs

### **"Necesito entender la lógica de motor"**

→ EPICA-4-Plan-Tecnico-TasaRetencion.md → KAN-14 → "Lógica de Simulación"  
→ Ejemplo código: En mismo doc, sección "Use Cases"

### **"Necesito crear UI (WPF)"**

→ EPICA-4-Plan-Tecnico-TasaRetencion.md → KAN-13 o KAN-15 → "UI (WPF)"  
→ Ejemplo patrón: src/Presentation/Views/InflacionView.xaml + ViewModel

### **"¿Cuál es mi bloqueador?"**

→ EPICA-4-Bloqueadores-Limitantes.md → sección "BLOQUEADORES CRÍTICOS"  
→ Si estás en KAN-14 Día 1: resuelve #1–4 primero.

### **"¿Qué permisos RBAC necesito?"**

→ EPICA-4-Plan-Tecnico-TasaRetencion.md → KAN-XX → "Permisos Necesarios"  
→ Crear en KAN-17 (post-release): TRE.CREAR, TRE.EDITAR, TRE.VER, TRE.ELIMINAR

### **"¿Qué tablas BD necesito?"**

→ EPICA-4-Plan-Tecnico-TasaRetencion.md → "Entidades de Dominio + BD" → sección "BD"  
→ SQL completo, índices, constraints.

### **"¿Cuál es el comando para..."**

→ EPICA-4-Resumen-Ejecutivo.md → "🔧 Comandos Rápidos"  
→ O EPICA-4-Plan-Tecnico-TasaRetencion.md → "Instrucciones para Claude Code" → "Comandos Rápidos"

---

## 📞 Soporte Rápido

### Antes de Empezar

```
✅ Build local funciona?
   cd D:\Tesis\SistemaAranceles
   dotnet build .\SistemaAranceles.sln
   → Debe ser 0 Errores, 0 Advertencias

✅ Ramas existen?
   git branch --list feature/KAN-1*
   → Debe mostrar KAN-13, 14, 15, 16

✅ Estás en develop?
   git checkout develop
   git pull origin develop
```

### Durante Desarrollo

```
❓ "Compilación falla en KAN-14"
  → Probablemente bloqueador #1–3. Ver EPICA-4-Bloqueadores-Limitantes.md

❓ "¿Por qué falla migración BD?"
  → Probablemente DbSet no agregado. Ver KAN-14 → "Tablas BD NO MIGRABLES"

❓ "¿Puedo usar transacción EF?"
  → Sí, ver patrón en EPICA-4-Plan-Tecnico-TasaRetencion.md → "Patrones de Implementación"

❓ "¿Debo crear pruebas?"
  → No es bloqueador KAN-13..16, pero SÍ para KAN-17 (release pre-prod).
```

### Post-Desarrollo

```
✅ Antes de commit:
   1. dotnet build → 0E/0W
   2. dotnet ef database update (KAN-14)
   3. Revisar cambios: git diff
   4. Commit con mensaje: "KAN-XX: Descripción (SP)"
   5. Push: git push origin feature/KAN-XX-*

✅ Antes de merge a develop:
   1. Pull request en GitHub (si existe)
   2. Code review (si existe)
   3. Merge, resolver conflictos si hay
   4. Pull develop: git checkout develop && git pull origin develop
   5. Continuar con siguiente KAN
```

---

## 🚨 Checklist Crítico

- [ ] **Leíste** EPICA-4-Resumen-Ejecutivo.md (5 min)
- [ ] **Entiendes** los 3 bloqueadores de KAN-14
- [ ] **Tienes** acceso a BD Supabase (test conexión)
- [ ] **Verificaste** que build funciona: `dotnet build`
- [ ] **Creaste/switcheaste** a feature/KAN-13-config-carrera-cohorte
- [ ] **Tienes** EPICA-4-Plan-Tecnico-TasaRetencion.md abierto
- [ ] **Entiendes** patrón Clean Architecture (use cases → repos → DTOs)

---

## 🎬 Ahora Sí: Inicia

```bash
cd D:\Tesis\SistemaAranceles

# Paso 1: Ir a KAN-13
git checkout feature/KAN-13-config-carrera-cohorte
git pull origin develop

# Paso 2: Crear carpeta estructura
mkdir -p src/Application/UseCases/TasaRetencion/KAN-13
mkdir -p src/Application/DTOs/TasaRetencion
mkdir -p src/Domain/Interfaces/Persistencia
mkdir -p src/Infrastructure/Persistence

# Paso 3: Empezar a codear
# Ver EPICA-4-Plan-Tecnico-TasaRetencion.md → KAN-13 → use cases

# Paso 4: Compilar + validar
dotnet build .\SistemaAranceles.sln

# Paso 5: Commit
git add .
git commit -m "KAN-13: Config carrera y cohorte (4 SP)"
git push origin feature/KAN-13-config-carrera-cohorte
```

---

## 📊 Progreso (Actualiza mientras avanzas)

| KAN | Repo | Use Cases | DTOs | Validators | UI  | Build | Commit |
| --- | ---- | --------- | ---- | ---------- | --- | ----- | ------ |
| 13  | ⏳   | ⏳        | ⏳   | ⏳         | ⏳  | ⏳    | ⏳     |
| 14  | ⏳   | ⏳        | ⏳   | ⏳         | —   | ⏳    | ⏳     |
| 15  | —    | ⏳        | ⏳   | —          | ⏳  | ⏳    | ⏳     |
| 16  | —    | ⏳        | ⏳   | —          | —   | ⏳    | ⏳     |

Legend: ⏳ = Pendiente | ✅ = Completo | — = N/A

---

## 📚 Referencias Rápidas

**Clean Architecture Pattern:** ctx.md → Sección 4 (arquitectura)  
**RBAC + Permisos:** contexto.md → Sección 4.3  
**Auditoría:** contexto.md → Sección 4.4  
**Inflación (dependencia):** contexto.md → Sección 4.5  
**Base de Datos:** KAN-03-modelo-db.md (tabla 29)  
**Ejemplo Use Case:** src/Application/UseCases/Inflacion/ProyectarInflacionUseCase.cs  
**Ejemplo Entity:** src/Domain/Entities/InflacionAnual.cs  
**Ejemplo UI:** src/Presentation/Views/InflacionView.xaml + ViewModel

---

## ✅ Listo. Adelante.

**Has recibido:**

1. ✅ 4 ramas funcionales
2. ✅ 4 documentos técnicos completos
3. ✅ Análisis de bloqueadores + plan de desbloqueo
4. ✅ Código ejemplo de patrones
5. ✅ Checklist de ejecución
6. ✅ Este índice de navegación

**Ahora:**
→ Lee EPICA-4-Resumen-Ejecutivo.md (5 min)  
→ Abre EPICA-4-Plan-Tecnico-TasaRetencion.md (referencia)  
→ `git checkout feature/KAN-13-config-carrera-cohorte`  
→ **Codea KAN-13 (4 SP, 3 días)**  
→ Commit + push  
→ Continúa KAN-14

**Éxito. 🚀**
