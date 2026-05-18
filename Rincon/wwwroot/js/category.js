var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblCategories').DataTable({
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
                        return `<span class="badge bg-success">Activo</span>`;
                    } else {
                        return `<span class="badge bg-danger">Inactivo</span>`;
                    }
                }
            },
            {
                data: "id",
                width: "25%",
                render: function (data) {
                    return `
                        <div class="text-center">
                            <a href="/Admin/Category/Upsert/${data}" class="btn btn-success text-white" style="cursor:pointer">
                                Editar
                            </a>
                            <a onclick=Delete("/Admin/Category/Delete/${data}") class="btn btn-danger text-white" style="cursor:pointer">
                                Eliminar
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