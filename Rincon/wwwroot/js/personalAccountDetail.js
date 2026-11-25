$(document).ready(function () {
    const saleDetailsTable = $('#personalAccountSaleDetailsTable');
    const paymentsTable = $('#personalAccountPaymentsTable');
    const accountId = saleDetailsTable.data('account-id');

    saleDetailsTable.DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Employee/PersonalAccounts/GetSaleDetails',
            type: 'GET',
            datatype: 'json',
            data: function (data) {
                data.id = accountId;
            }
        },
        columns: [
            { data: 'date' },
            {
                data: 'saleNumber',
                render: function (data, type, row) {
                    if (!row.saleDetailUrl) {
                        return `<span class="fw-bold text-primary">${data}</span>`;
                    }

                    return `<a href="${row.saleDetailUrl}" class="fw-bold text-primary text-decoration-none">${data}</a>`;
                }
            },
            {
                data: 'product',
                render: function (data) {
                    return `<span class="fw-semibold">${data}</span>`;
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
                render: function (data) {
                    return `<span class="fw-bold">$ ${data}</span>`;
                }
            },
            {
                data: 'status',
                render: function (data, type, row) {
                    return `<span class="status-badge ${row.statusClass}">${data}</span>`;
                }
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']]
    });

    paymentsTable.DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Employee/PersonalAccounts/GetPayments',
            type: 'GET',
            datatype: 'json',
            data: function (data) {
                data.id = accountId;
            }
        },
        columns: [
            { data: 'date' },
            { data: 'paymentMethod' },
            {
                data: 'amount',
                render: function (data, type, row) {
                    if (row.isVoided) {
                        return `<span class="fw-bold text-danger text-decoration-line-through">$ ${data}</span>`;
                    }

                    return `<span class="fw-bold text-success">$ ${data}</span>`;
                }
            },
            { data: 'cashRegister' },
            { data: 'user' },
            {
                data: 'notes',
                render: function (data, type, row) {
                    const hasNotes = Boolean(data && data !== '-');
                    const notes = hasNotes ? escapeHtml(data) : '';

                    if (!row.isVoided) {
                        return hasNotes ? notes : '-';
                    }

                    const voidReason = escapeHtml(row.voidReason || 'Sin motivo informado');
                    const voidedBy = escapeHtml(row.voidedBy || 'Sin usuario');
                    const voidedAt = escapeHtml(row.voidedAt || '-');

                    const voidReasonClass = hasNotes ? 'small text-danger mt-1' : 'small text-danger';

                    return `${notes}<div class="${voidReasonClass}">Anulado: ${voidReason}</div>`
                        + `<div class="small text-muted">${voidedAt} · ${voidedBy}</div>`;
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                className: 'text-center text-nowrap',
                render: function (data, type, row) {
                    if (row.isVoided) {
                        return '<span class="status-badge status-inactive">Anulado</span>';
                    }

                    if (!row.canVoid) {
                        return '<span class="text-muted">-</span>';
                    }

                    return `<button type="button"`
                        + ` class="btn btn-sm btn-soft-danger"`
                        + ` data-bs-toggle="modal"`
                        + ` data-bs-target="#voidPersonalAccountPaymentModal"`
                        + ` data-payment-id="${row.id}"`
                        + ` data-payment-amount="${escapeHtml(row.amount)}"`
                        + ` data-payment-method="${escapeHtml(row.paymentMethod)}"`
                        + ` data-payment-cash-register="${escapeHtml(row.cashRegister)}">`
                        + '<i class="fa fa-ban me-1"></i> Anular'
                        + '</button>';
                }
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']]
    });

    const voidPaymentModal = document.getElementById('voidPersonalAccountPaymentModal');

    if (voidPaymentModal) {
        voidPaymentModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const paymentMethod = button?.getAttribute('data-payment-method') || '';
            const paymentId = button?.getAttribute('data-payment-id') || '';
            const amount = button?.getAttribute('data-payment-amount') || '';
            const cashRegister = button?.getAttribute('data-payment-cash-register') || '';

            document.getElementById('voidPaymentId').value = paymentId;
            document.getElementById('voidPaymentAmount').textContent = `Pago: $ ${amount}`;
            document.getElementById('voidPaymentMethod').textContent = `Medio: ${paymentMethod}`;
            document.getElementById('voidPaymentCashRegister').textContent = cashRegister;
            document.getElementById('voidPaymentReason').value = '';

        });
    }
});

function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}
