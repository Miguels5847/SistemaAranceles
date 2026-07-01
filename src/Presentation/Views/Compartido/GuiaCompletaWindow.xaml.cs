using System.Windows;

namespace SistemaAranceles.Presentation.Views.Compartido;

public partial class GuiaCompletaWindow : Window
{
    public sealed record PasoGuia(int Numero, string Modulo, string Ubicacion, string Acciones, string PorQue, string ListoCuando);

    public GuiaCompletaWindow()
    {
        InitializeComponent();

        // No exceder el área de trabajo del monitor.
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width, area.Width);
        Height = Math.Min(Height, area.Height);

        ListaPasos.ItemsSource = Pasos;
    }

    // Guía del flujo completo para el usuario final (sin módulos de administrador).
    // Los nombres de botones y pestañas son los reales de cada vista: si se renombra un botón,
    // actualizar aquí también.
    private static readonly PasoGuia[] Pasos =
    [
        new(1, "Carreras", "Menú: 1 · Configuración base → Carreras",
            "Pulsa \"Nuevo\" (limpia el formulario) → escribe el Código, Nombre, Facultad y Total de ciclos de la carrera → pulsa \"Guardar\". Si te equivocas, selecciona la carrera en la lista y usa \"Editar selección\".",
            "Todo el sistema calcula POR carrera: estudiantes, costos y arancel se guardan bajo la carrera que registres aquí. Sin este paso no hay dónde trabajar.",
            "la carrera aparece en la lista con su código y número de ciclos."),

        new(2, "Tasa de Retención y Graduación", "Menú: 1 · Configuración base → Tasa de Retención y Graduación",
            "En la pestaña \"Escenarios\": elige la Carrera → elige el escenario \"Histórico\" → escribe la Meta de retención % (ej. 65) y la Meta de graduación % (ej. 80); debajo verás la tasa por ciclo que el sistema deriva de tus metas → completa Estudiantes y Paralelos de los períodos 1 y 2 → pulsa \"Guardar\". Repite para \"Optimista\" y \"Pesimista\" (los valores se precargan solos desde el Histórico). Después, en la pestaña \"Simulación\": elige la configuración → pulsa \"Ejecutar simulación\".",
            "Las metas definen cuántos estudiantes avanzan de un ciclo al siguiente. De aquí salen los estudiantes que pagan, los docentes necesarios y, al final, el arancel.",
            "la simulación muestra los cuadros verdes con los alumnos que quedan en cada ciclo."),

        new(3, "Proyección de Estudiantes", "Menú: 2 · Proyección académica → Proyección de Estudiantes",
            "Selecciona la carrera y el escenario → pulsa \"GENERAR PROYECCIÓN\" (crea la matriz de estudiantes por período y ciclo). Si cambiaste las metas del paso 2, pulsa \"REFRESCAR\" y vuelve a generar.",
            "Esta matriz es la base de la demanda (cuántos pagan cada semestre) y del número de docentes que la carrera necesita.",
            "se ve la matriz con las cohortes avanzando y los totales por período."),

        new(4, "Recursos y Depreciación", "Menú: 3 · Costos y recursos → Recursos y Depreciación",
            "Pulsa \"Generar activos por defecto\" (crea la lista típica de equipos, mobiliario y laboratorio) y ajusta cantidades o precios con \"Editar selección\"; o usa \"Nuevo\" para agregar un activo puntual → \"Guardar\" → pulsa \"Cargar / Recalcular depreciación\" para actualizar la tabla.",
            "Los activos definen la inversión inicial de la carrera, y su depreciación anual es parte del costo que el arancel debe cubrir.",
            "hay activos en la lista y la tabla de depreciación muestra valores por año."),

        new(5, "Mantenimiento e Inversión", "Menú: 3 · Costos y recursos → Mantenimiento e Inversión",
            "Pestaña \"Mantenimiento\": pulsa \"Generar servicios por defecto\" (agua, luz, internet…) o \"+ Nuevo\" para uno específico. Pestaña \"Activos Diferidos\": registra licencias y permisos si aplican. Pestaña \"Inversión Inicial\": solo revisa el consolidado (se arma solo con lo de los pasos 4 y 5).",
            "Completa el costo operativo mensual y cierra la inversión inicial total que el análisis financiero usará para calcular el préstamo y el VAN.",
            "la pestaña Inversión Inicial muestra el total consolidado sin ceros raros."),

        new(6, "Demanda e Ingresos", "Menú: 2 · Proyección académica → Demanda e Ingresos",
            "Pestaña \"1. Configuración de Arancel\": elige el modo de arancel — recomendado el óptimo; el botón \"Usar arancel sugerido\" copia el valor calculado. Pestaña \"5. Materiales en Cantidades\": pulsa \"Generar consumos por defecto\" y ajusta; \"+ Nuevo consumo\" agrega un material puntual. Si la carrera da descuentos por ciclo: \"Editar descuentos\" → escribe los % → \"Guardar descuentos\".",
            "Aquí se define cuánto se cobra por semestre y cuánto material se consume: de esto salen los ingresos proyectados de la carrera.",
            "la pestaña \"4. Ingresos Proyectados\" muestra ingresos por período distintos de cero."),

        new(7, "Costos y Gastos", "Menú: 3 · Costos y recursos → Costos y Gastos",
            "No hay nada que llenar: solo elige carrera y escenario. Revisa la pestaña \"2. Costos y Gastos\" (total por período) y la \"3. Costo de la carrera (referencial)\" (cuánto cuesta formar a un estudiante toda la carrera).",
            "Es el punto de control: junta sueldos, materiales, mantenimiento y depreciación. Si un valor se ve gigante o en cero, el error está en los pasos anteriores.",
            "el costo por estudiante se ve razonable (ni cero ni cientos de miles)."),

        new(8, "Análisis Financiero", "Menú: 4 · Financiamiento y análisis → Análisis Financiero",
            "Elige carrera y escenario y revisa: pestaña \"3. TIR / VAN\" (la viabilidad), pestaña \"6. Arancel Óptimo\" (el arancel que hace VAN ≈ 0: es el valor a cobrar para que la carrera se pague sola) y pestaña \"8. CES / INF CES\" (los cuadros para justificar el arancel ante el CES).",
            "Aquí se decide el arancel final. Si la TIR supera la TMR y el VAN con el arancel óptimo queda cerca de 0, la carrera es viable.",
            "el VAN ≈ 0 con el arancel óptimo y la TIR es mayor o igual a la TMR."),

        new(9, "Reportes", "Menú: 5 · Resultados → Reportes",
            "Elige carrera, escenario y la dirección destinataria → pulsa \"Exportar PDF\" o \"Exportar XLSX\". El informe empieza con el \"Resumen de Indicadores Clave\" (estudiantes, costo por estudiante, arancel, VAN, TIR y punto de equilibrio).",
            "Es el entregable final: el documento con el que se presenta y sustenta el arancel calculado.",
            "el archivo PDF o Excel se genera y abre sin errores."),
    ];
}
