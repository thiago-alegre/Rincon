$(document).ready(function () {
    const table = $('#cashRegisterDetailTable');
    const sessionId = table.data('session-id');

    table.DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: `/Employee/CashRegister/GetDetailItems?id=${sessionId}`,
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            {
                data: 'date',
                render: function (data, type, row) {
                    return type === 'sort' ? row.dateSort : data;
                }
            },
            {
                data: 'saleNumber',
                render: function (data) {
                    return `<span class="fw-bold text-primary">${data}</span>`;
                }
            },
            {
                data: 'paymentMethod',
                render: function (data, type, row) {
                    return `<span class="status-badge ${row.movementStatusClass}">${row.movementStatus}</span>`;
                }
            },
            {
                data: 'paymentMethod',
                render: function (data) {
                    return renderPaymentBadge(data);
                }
            },
            {
                data: null,
                render: function (data) {
                    const code = data.articleCode ? `<div class="small text-muted">Código: ${data.articleCode}</div>` : '';
                    return `<div class="fw-semibold">${data.articleName}</div>${code}`;
                }
            },
            { data: 'quantity' },
            {
                data: 'unitPrice',
                render: function (data) {
                    return `$ ${data}`;
                }
            },
            {
                data: 'subtotal',
                render: function (data, type, row) {
                    const className = row.subtotalValue < 0 || row.movementStatus === 'Recambio'
                        ? 'text-danger'
                        : '';
                    return `<span class="fw-bold ${className}">$ ${data}</span>`;
                }
            },
            {
                data: null,
                render: function (data) {
                    if (!data.personalAccountUrl) {
                        return data.personalAccount;
                    }

                    return `<a href="${data.personalAccountUrl}" class="fw-semibold text-decoration-none">${data.personalAccount}</a>`;
                }
            },
            {
                data: 'debtStatus',
                orderable: false,
                render: function (data, type, row) {
                    if (data === '-') {
                        return '<span class="status-badge expiration-empty">No aplica</span>';
                    }

                    const className = row.debtSettled ? 'status-active' : 'expiration-danger';
                    return `<span class="status-badge ${className}">${data}</span>`;
                }
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']]
    });
});

function renderPaymentBadge(paymentMethod) {
    if (paymentMethod === 'Efectivo') {
        return '<span class="payment-badge payment-cash">Efectivo</span>';
    }

    if (paymentMethod === 'Transferencia') {
        return '<span class="payment-badge payment-transfer">Transferencia</span>';
    }

    if (paymentMethod === 'Sin movimiento de caja') {
        return '<span class="payment-badge expiration-empty">Sin caja</span>';
    }

    return '<span class="payment-badge expiration-warning">Cuenta personal</span>';
}
