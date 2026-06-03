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
                    const className = row.settled ? 'status-active' : 'status-inactive';
                    return `<span class="status-badge ${className}">${data}</span>`;
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
                render: function (data) {
                    return `<span class="fw-bold text-success">$ ${data}</span>`;
                }
            },
            { data: 'cashRegister' },
            { data: 'user' },
            { data: 'notes' }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']]
    });
});
