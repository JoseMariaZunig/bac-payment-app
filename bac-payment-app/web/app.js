(function () {
    "use strict";

    const API_BASE = (window.APP_CONFIG && window.APP_CONFIG.API_BASE_URL) || "http://localhost:8080";

    const state = {
        token: null,
        nombreTransaccion: null,
        categoriaPago: null,
        montoTotal: null,
        beneficiarios: null
    };

    const els = {
        consoleBody: document.getElementById("consoleBody"),
        statusDot: document.getElementById("statusDot"),
        statusText: document.getElementById("statusText"),
        btnToken: document.getElementById("btnToken"),
        btnSoftland: document.getElementById("btnSoftland"),
        btnPago: document.getElementById("btnPago"),
        btnRastreo: document.getElementById("btnRastreo"),
        btnClear: document.getElementById("btnClear"),
        tipoReporte: document.getElementById("tipoReporte"),
        fechaDesde: document.getElementById("fechaDesde"),
        fechaHasta053: document.getElementById("fechaHasta053"),
        fechaHastaWrapper: document.getElementById("fechaHastaWrapper"),
        fechasReporte: document.getElementById("fechasReporte"),
        labelFechaDesde: document.getElementById("labelFechaDesde"),
        reporteHint: document.getElementById("reporteHint"),
        btnConsultarReporte: document.getElementById("btnConsultarReporte"),
        txSummary: document.getElementById("txSummary"),
        txId: document.getElementById("txId"),
        txCat: document.getElementById("txCat"),
        txTotal: document.getElementById("txTotal")
    };

    function log(message, level) {
        const line = document.createElement("div");
        line.className = "log-line log-" + (level || "info");
        line.textContent = message;
        els.consoleBody.appendChild(line);
        els.consoleBody.scrollTop = els.consoleBody.scrollHeight;
    }

    function divider() {
        log("───────────────────────────────", "divider");
    }

    function setStatus(kind, text) {
        els.statusDot.className = "status-dot " + kind;
        els.statusText.textContent = text;
    }

    function setBusy(button, busyText, originalText) {
        button.disabled = true;
        button.dataset.originalText = originalText;
        button.textContent = busyText;
    }

    function clearBusy(button) {
        button.disabled = false;
        button.textContent = button.dataset.originalText || button.textContent;
    }

    async function apiPost(path, body) {
        const res = await fetch(API_BASE + path, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: body ? JSON.stringify(body) : undefined
        });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
            const err = new Error(data.mensaje || "Error de red");
            err.detalle = data.detalle;
            throw err;
        }
        return data;
    }

    // --- 1. Obtener token -------------------------------------------------
    els.btnToken.addEventListener("click", async () => {
        setBusy(els.btnToken, "Obteniendo token...", "Obtener token");
        setStatus("busy", "Obteniendo token...");
        try {
            const data = await apiPost("/api/token/obtain");
            state.token = data.token;
            log("Se obtuvo el token correctamente", "success");
            setStatus("ok", "Token activo");
        } catch (ex) {
            log("Error obteniendo token: " + ex.message, "error");
            if (ex.detalle) log(ex.detalle, "muted");
            setStatus("err", "Error de token");
        } finally {
            clearBusy(els.btnToken);
            els.btnToken.textContent = "Obtener token";
        }
    });

    // --- 2. Consultar Softland ---------------------------------------------
    els.btnSoftland.addEventListener("click", async () => {
        const tipoPago = document.querySelector('input[name="tipoPago"]:checked').value;

        setBusy(els.btnSoftland, "Consultando...", "Consultar Softland");
        try {
            const data = await apiPost("/api/softland/consultar", { tipoPago });

            state.nombreTransaccion = data.nombreTransaccion;
            state.categoriaPago = data.categoriaPago;
            state.montoTotal = data.montoTotal;
            state.beneficiarios = data.beneficiarios;

            divider();
            log("Información obtenida:", "info");
            divider();
            data.beneficiarios.forEach((b) => {
                log(`  Cliente : ${b.cliente}`, "success");
                log(`  Monto   : CRC ${b.monto}`, "success");
                log(`  Cuenta destino : ${b.cuentaDestino}`, "success");
            });
            divider();

            els.txSummary.hidden = false;
            els.txId.textContent = data.nombreTransaccion;
            els.txCat.textContent = data.categoriaPago;
            els.txTotal.textContent = "CRC " + data.montoTotal.toFixed(2);

            els.btnPago.disabled = false;
        } catch (ex) {
            log("Error consultando Softland: " + ex.message, "error");
            if (ex.detalle) log(ex.detalle, "muted");
        } finally {
            clearBusy(els.btnSoftland);
            els.btnSoftland.textContent = "Consultar Softland";
        }
    });

    // --- 3. Realizar pago ---------------------------------------------------
    els.btnPago.addEventListener("click", async () => {
        if (!state.token) {
            log("Primero obtén un token con el botón 'Obtener token'", "warn");
            return;
        }
        if (!state.beneficiarios) {
            log("Primero consulta los datos de Softland", "warn");
            return;
        }

        setBusy(els.btnPago, "Procesando pago...", "Realizar pago");
        try {
            const data = await apiPost("/api/payment/procesar", {
                token: state.token,
                nombreTransaccion: state.nombreTransaccion,
                categoriaPago: state.categoriaPago,
                montoTotal: state.montoTotal,
                beneficiarios: state.beneficiarios
            });

            divider();
            log("Información del pago", "info");
            divider();

            if (data.evtCd === "RCVD") {
                log("PAGO ENVIADO - Procesándose", "success");
                log(`Identificador del pago: ${state.nombreTransaccion}`, "success");
                log(`EvtCd: ${data.evtCd}`, "success");
                log(`  ${data.evtDesc}`, "success");
                els.btnRastreo.disabled = false;
            } else if (data.evtCd === "RJCT") {
                log("PAGO RECHAZADO", "error");
                log(`EvtCd: ${data.evtCd}`, "error");
                log(`  ${data.evtDesc}`, "error");
            } else {
                log(`EvtCd: ${data.evtCd} — ${data.evtDesc}`, "warn");
                els.btnRastreo.disabled = false;
            }
        } catch (ex) {
            log("Error procesando pago: " + ex.message, "error");
            if (ex.detalle) log(ex.detalle, "muted");
        } finally {
            clearBusy(els.btnPago);
            els.btnPago.textContent = "Realizar pago";
        }
    });

    // --- 4. Rastrear pago ----------------------------------------------------
    els.btnRastreo.addEventListener("click", async () => {
        if (!state.token || !state.nombreTransaccion) {
            log("Necesitas un token y una transacción enviada antes de rastrear", "warn");
            return;
        }

        setBusy(els.btnRastreo, "Consultando...", "Rastrear pago");
        try {
            const data = await apiPost("/api/payment/rastrear", {
                token: state.token,
                nombreTransaccion: state.nombreTransaccion
            });

            divider();
            log(`Rastreo: ${state.nombreTransaccion}`, "info");
            divider();

            if (data.enProceso) {
                log(`Estado: ${data.evtCd} — todavía en proceso`, "warn");
                log(`  ${data.evtDesc}`, "muted");
                log("Vuelve a rastrear en unos minutos para ver el detalle por beneficiario.", "muted");
            } else if (data.transacciones && data.transacciones.length > 0) {
                data.transacciones.forEach((tx) => {
                    const level = tx.txSts === "ACTC" ? "success" : tx.txSts === "RJCT" ? "error" : "warn";
                    log(`  Cliente : ${tx.cliente}`, "success");
                    log(`  Monto   : ${tx.moneda} ${tx.monto}`, "success");
                    log(`  Estado  : ${tx.estado}`, level);
                    log(`  ───────────────────────────────`, "muted");
                });
            } else {
                log("Sin información de transacciones disponible todavía", "muted");
            }
        } catch (ex) {
            log("Error en rastreo: " + ex.message, "error");
            if (ex.detalle) log(ex.detalle, "muted");
        } finally {
            clearBusy(els.btnRastreo);
            els.btnRastreo.textContent = "Rastrear pago";
        }
    });

    els.btnClear.addEventListener("click", () => {
        els.consoleBody.innerHTML = "";
        log("Consola limpiada", "muted");
    });

    // --- 5. Reportes de cuenta (selector único) -------------------------------

    const HINTS = {
        camt053: "",
        mt940: "",
        mt942: "",
        saldo: ""
    };

    function actualizarVisibilidadFechas() {
        const tipo = els.tipoReporte.value;
        els.reporteHint.textContent = HINTS[tipo];

        if (tipo === "mt942" || tipo === "saldo") {
            els.fechasReporte.classList.add("hidden");
        } else {
            els.fechasReporte.classList.remove("hidden");
        }

        if (tipo === "camt053") {
            els.fechaHastaWrapper.classList.remove("hidden");
        } else {
            els.fechaHastaWrapper.classList.add("hidden");
        }
    }

    els.tipoReporte.addEventListener("change", actualizarVisibilidadFechas);
    actualizarVisibilidadFechas();

    async function consultarCamt053() {
        const fecha = els.fechaDesde.value;
        if (!fecha) { log("Selecciona una fecha 'Desde' para consultar", "warn"); return; }
        const fechaHasta = els.fechaHasta053.value || fecha;
        if (fechaHasta < fecha) { log("La fecha 'Hasta' no puede ser anterior a la fecha 'Desde'", "warn"); return; }

        const data = await apiPost("/api/statement/camt053", {
            token: state.token, fechaDesde: fecha, fechaHasta: fechaHasta, pagina: "1"
        });

        divider();
        log(`camt.053: ${data.cuenta} (${data.moneda})`, "info");
        divider();
        log(`ID estado       : ${data.idEstado}`, "success");
        log(`Página          : ${data.pagina} ${data.ultimaPagina ? "(última)" : "(hay más páginas)"}`, "success");
        log(`Saldo apertura  : ${data.saldoApertura} (${data.saldoAperturaIndicador})`, "success");
        log(`Saldo cierre    : ${data.saldoCierre} (${data.saldoCierreIndicador})`, "success");

        if (data.movimientos && data.movimientos.length > 0) {
            divider();
            log(`Movimientos (${data.movimientos.length}):`, "info");
            data.movimientos.forEach((m, i) => {
                log(`  [${i + 1}] ${m.fecha} | ${m.indicador} | ${m.moneda} ${m.monto}`, "success");
                if (m.descripcion) log(`      ${m.descripcion}`, "muted");
            });
        } else {
            log("Sin movimientos en el rango consultado", "muted");
        }
        divider();
    }

    async function consultarMt940() {
        const fecha = els.fechaDesde.value;
        if (!fecha) { log("Selecciona una fecha para consultar", "warn"); return; }

        // BAC no soporta rango en MT940 (solo usa "FechaDesde"), así que mandamos
        // la misma fecha en ambos campos y el campo "Hasta" del formulario se ignora aquí.
        const data = await apiPost("/api/statement/consultar", {
            token: state.token, fechaDesde: fecha, fechaHasta: fecha, pagina: "1"
        });

        divider();
        log(`Estado de cuenta: ${data.cuenta}`, "info");
        divider();
        log(`Referencia         : ${data.referencia}`, "success");
        log(`No. de estado      : ${data.numeroEstado}`, "success");
        log(`Saldo apertura     : ${data.saldoApertura}`, "success");
        log(`Saldo cierre       : ${data.saldoCierre}`, "success");
        log(`Saldo disp. cierre : ${data.saldoDisponibleCierre}`, "success");
        log(`Saldo disp. futuro : ${data.saldoDisponibleFuturo}`, "success");
        if (data.informacionAdicional) {
            log(`Info adicional     : ${data.informacionAdicional}`, "muted");
        }

        if (data.movimientos && data.movimientos.length > 0) {
            divider();
            log(`Movimientos (${data.movimientos.length}):`, "info");
            data.movimientos.forEach((m, i) => {
                log(`  [${i + 1}] ${m.linea61}`, "success");
                log(`      ${m.descripcion86}`, "muted");
            });
        } else {
            log("Sin movimientos en el rango consultado", "muted");
        }
        divider();
    }

    async function consultarMt942() {
        const data = await apiPost("/api/statement/intradia", { token: state.token });

        divider();
        log(`Movimientos de hoy: ${data.cuenta}`, "info");
        divider();
        log(`Referencia      : ${data.referencia}`, "success");
        log(`Límite flotante : ${data.limiteFloor}`, "success");
        log(`Fecha/hora      : ${data.fechaHoraIndicacion}`, "success");

        if (data.movimientos && data.movimientos.length > 0) {
            divider();
            log(`Movimientos (${data.movimientos.length}):`, "info");
            data.movimientos.forEach((m, i) => {
                log(`  [${i + 1}] ${m.linea61}`, "success");
                log(`      ${m.descripcion86}`, "muted");
            });
        } else {
            log("Sin movimientos hoy", "muted");
        }

        divider();
        log(`Total débitos  : ${data.totalDebitos}`, "muted");
        log(`Total créditos : ${data.totalCreditos}`, "muted");
        divider();
    }

    async function consultarSaldo() {
        const data = await apiPost("/api/statement/saldo", { token: state.token });

        divider();
        log(`Saldo de la cuenta: ${data.cuenta}`, "info");
        divider();
        log(`Tipo de saldo : ${data.tipoSaldo}`, "success");
        log(`Indicador     : ${data.indicador === "CRDT" ? "A favor (crédito)" : "En contra (débito)"}`, "success");
        log(`Fecha         : ${data.fecha}`, "success");
        log(`Monto         : ${data.moneda} ${data.monto}`, "success");
        divider();
    }

    const REPORTES = {
        camt053: { fn: consultarCamt053, texto: "Estado de cuenta (camt.053)" },
        mt940: { fn: consultarMt940, texto: "Estado de cuenta (MT940)" },
        mt942: { fn: consultarMt942, texto: "Movimientos de hoy (MT942)" },
        saldo: { fn: consultarSaldo, texto: "Saldo actual" }
    };

    els.btnConsultarReporte.addEventListener("click", async () => {
        if (!state.token) {
            log("Primero obtén un token con el botón 'Obtener token'", "warn");
            return;
        }

        const tipo = els.tipoReporte.value;
        const reporte = REPORTES[tipo];

        setBusy(els.btnConsultarReporte, "Consultando...", "Consultar");
        try {
            await reporte.fn();
        } catch (ex) {
            log(`Error consultando ${reporte.texto}: ` + ex.message, "error");
            if (ex.detalle) log(ex.detalle, "muted");
        } finally {
            clearBusy(els.btnConsultarReporte);
            els.btnConsultarReporte.textContent = "Consultar";
        }
    });
})();