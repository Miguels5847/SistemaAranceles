# Caso de Uso: Generar Flujo de Fondos Neto

**Identificador:** CU-AF-02  
**Nombre:** Generar Flujo de Fondos Neto  
**RF asociado:** RF-AF-02

**Actor primario:** Sistema  
**Actor secundario:** Planificador Académico / Consultor

## Descripción

El sistema genera automáticamente el flujo de fondos neto proyectado incluyendo el año 0.

# 1. Flujo Principal

1. El sistema toma:
   - Utilidad (CU-AF-01)
   - Inversiones (CU-RD-02)
   - Depreciación (CU-RD-03)
   - Amortización (CU-MI-04)
   - Pago crédito (CU-FB-01)
   - Inversión inicial (CU-MI-03)
2. Año 0:
   - Flujo = −Inversión_inicial
3. Períodos siguientes:
   - Flujo = Utilidad − Inversiones + Depreciación + Amortización − Pago_Crédito
4. El sistema muestra tabla con año 0 incluido.

## Flujos alternos

Datos parciales:

- El sistema calcula con valores disponibles y completa con $0

# Precondiciones

- P&G generado (CU-AF-01)
- Inversión inicial calculada (CU-MI-03)

# Postcondiciones

- Flujo disponible para TIR y VAN

# Reglas de negocio

RN-161: Año 0 = −Inversión_inicial  
RN-162: Flujo = Utilidad − Inversiones + Depreciación + Amortización − Pago_Crédito
