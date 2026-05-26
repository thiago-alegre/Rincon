var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblCategories').DataTable({
        pageLength: 5,
        ajax: {
            url: "/Admin/Category/GetAll"
        },
        columns: [
            { data: "name", width: "30%" },
            {
                data: "date",
                width: "25%",
                render: function (data) {
                    if (!data) return "";
                    const fecha = new Date(data);
                    return fecha.toLocaleDateString("es-AR");
                }
            },
            {
                data: "isActive",
                width: "20%",
                render: function (data) {
                    if (data === true) {
                        return `<span class="status-badge status-active">Activo</span>`;
                    } else {
                        return `<span class="status-badge status-inactive">Inactivo</span>`;
                    }
                }
            },
            {
                data: "id",
                width: "25%",
                render: function (data) {
                    return `
                        <div class="datatable-action-group">
                            <a href="/Admin/Category/Upsert/${data}" class="btn btn-soft-success btn-modern-sm" title="Editar categoría">
                                <i class="fa fa-pencil me-1" aria-hidden="true"></i> Editar
                            </a>
                            <a onclick=Delete("/Admin/Category/Delete/${data}") class="btn btn-soft-danger btn-modern-sm" title="Eliminar categoría">
                                <i class="fa fa-trash me-1" aria-hidden="true"></i> Eliminar
                            </a>
                        </div>
                    `;
                }
            }
        ],
        language: {
            decimal: "",
            emptyTable: "No hay datos disponibles",
            info: "Mostrando _START_ a _END_ de _TOTAL_ registros",
            infoEmpty: "Mostrando 0 a 0 de 0 registros",
            infoFiltered: "(filtrado de _MAX_ registros totales)",
            lengthMenu: "Mostrar _MENU_ registros",
            loadingRecords: "Cargando...",
            processing: "Procesando...",
            search: "Buscar:",
            zeroRecords: "No se encontraron registros",
            paginate: {
                first: "Primero",
                last: "Último",
                next: "Siguiente",
                previous: "Anterior"
            }
        }
    });
}

function Delete(url) {
    Swal.fire({
        title: "¿Está seguro?",
        text: "La categoría se eliminará permanentemente",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#d33",
        cancelButtonColor: "#6c757d",
        confirmButtonText: "Sí, eliminar",
        cancelButtonText: "Cancelar"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}
