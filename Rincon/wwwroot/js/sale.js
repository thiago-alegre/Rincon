$(document).ready(function () {
    $('#salesTable').DataTable({
        ajax: {
            url: '/Admin/Sales/GetAll',
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            { data: 'date' },
            { data: 'paymentMethod' },
            {
                data: 'total',
                render: function (data) {
                    return '$ ' + data;
                }
            },
            { data: 'user' },
            {
                data: 'detailUrl',
                render: function (data) {
                    return `
                        <div class="text-end">
                            <a href="${data}" class="btn btn-sm btn-outline-primary rounded-pill">
                                <i class="fa fa-eye me-1"></i> Ver detalle
                            </a>
                        </div>
                    `;
                },
                orderable: false
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        },
        order: [[0, 'desc']]
    });
});