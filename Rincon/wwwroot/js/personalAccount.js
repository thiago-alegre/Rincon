$(document).ready(function () {
    $('#personalAccountsTable').DataTable({
        processing: true,
        serverSide: true,
        pageLength: 5,
        ajax: {
            url: '/Employee/PersonalAccounts/GetAll',
            type: 'GET',
            datatype: 'json'
        },
        columns: [
            { data: 'fullName' },
            { data: 'dni' },
            { data: 'phone', render: data => data || '-' },
            { data: 'address', render: data => data || '-' },
            {
                data: 'debt',
                render: function (data, type, row) {
                    const debtClass = row.debtValue > 0 ? 'text-danger' : 'text-success';
                    return `<span class="fw-bold ${debtClass}">$ ${data}</span>`;
                }
            },
            { data: 'debtSince' },
            {
                data: null,
                render: function (data) {
                    let buttons = `
                        <div class="datatable-action-group justify-content-end">
                            <a href="${data.detailUrl}" class="btn btn-soft-secondary btn-modern-sm">
                                <i class="fa fa-eye me-1"></i> Ver detalle
                            </a>
                    `;

                    if (data.canManage) {
                        buttons += `
                            <a href="${data.editUrl}" class="btn btn-soft-secondary btn-modern-sm">
                                <i class="fa fa-pen me-1"></i> Editar
                            </a>
                            <button type="button" class="btn btn-soft-danger btn-modern-sm" onclick="deletePersonalAccount(${data.id})">
                                <i class="fa fa-trash me-1"></i> Eliminar
                            </button>
                        `;
                    }

                    buttons += '</div>';
                    return buttons;
                },
                orderable: false
            }
        ],
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json'
        }
    });
});

function deletePersonalAccount(id) {
    rinconConfirm({
        title: '¿Eliminar cuenta personal?',
        text: 'La cuenta se desactivará si no tiene deuda pendiente.',
        icon: 'warning',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar'
    }).then(result => {
        if (!result.isConfirmed) {
            return;
        }

        $.ajax({
            url: `/Employee/PersonalAccounts/Delete/${id}`,
            type: 'DELETE',
            success: function (data) {
                if (data.success) {
                    toastr.success(data.message);
                    $('#personalAccountsTable').DataTable().ajax.reload();
                } else {
                    toastr.error(data.message);
                }
            }
        });
    });
}
