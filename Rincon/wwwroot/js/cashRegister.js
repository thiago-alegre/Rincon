$(document).ready(function () {
    $('#cashRegisterTable').DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Employee/CashRegister/GetAll',
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            {
                data: 'user',
                render: function (data) {
                    return `<span class="fw-semibold">${data}</span>`;
                }
            },
            {
                data: 'openedAt',
                render: function (data, type, row) {
                    return type === 'sort' ? row.openedAtSort : data;
                }
            },
            {
                data: 'closedAt',
                render: function (data, type, row) {
                    return type === 'sort' ? row.closedAtSort : data;
                }
            },
            {
                data: 'openingAmount',
                render: function (data) {
                    return `$ ${data}`;
                }
            },
            {
                data: 'expectedCashAmount',
                render: function (data) {
                    return data === '-' ? '-' : `$ ${data}`;
                }
            },
            {
                data: 'countedCashAmount',
                render: function (data) {
                    return data === '-' ? '-' : `$ ${data}`;
                }
            },
            {
                data: 'difference',
                render: function (data, type, row) {
                    if (type === 'sort') {
                        return row.differenceValue;
                    }

                    if (data === '-') {
                        return '-';
                    }

                    const className = row.differenceValue < 0
                        ? 'text-danger'
                        : row.differenceValue > 0
                            ? 'text-success'
                            : '';

                    return `<span class="fw-bold ${className}">$ ${data}</span>`;
                }
            },
            {
                data: 'status',
                render: function (data, type, row) {
                    const className = row.isOpen ? 'status-active' : 'status-inactive';
                    return `<span class="status-badge ${className}">${data}</span>`;
                }
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[1, 'desc']]
    });
});
