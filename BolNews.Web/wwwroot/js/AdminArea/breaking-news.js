$(document).ready(function () {

    initializeDataTable();
    initializeSelect2();
    wireDelete();
    wireActivateDeactivate();
    wireFilters();

});

function initializeDataTable() {

    if ($.fn.DataTable.isDataTable("#breakingNewsTable")) {
        return;
    }

    $("#breakingNewsTable").DataTable({
        responsive: true,
        pageLength: 25,
        autoWidth: false,
        order: [[3, "asc"]],
        language: {
            search: "",
            searchPlaceholder: "Search..."
        }
    });
}

function initializeSelect2() {

    if ($("#ArticleId").length === 0)
        return;

    $("#ArticleId").select2({

        placeholder: "Search published article...",
        minimumInputLength: 2,
        allowClear: true,

        ajax: {

            url: "/Admin/BreakingNews/SearchArticles",

            dataType: "json",

            delay: 300,

            data: function (params) {

                return {
                    term: params.term
                };

            },

            processResults: function (data) {

                return {
                    results: data
                };

            }

        }

    });

}

function wireDelete() {

    $(document).on("click", ".deleteBtn", function () {

        let id = $(this).data("id");

        Swal.fire({

            title: "Delete Breaking News?",
            text: "This action cannot be undone.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#dc3545",
            confirmButtonText: "Delete"

        }).then(function (result) {

            if (!result.isConfirmed)
                return;

            $.ajax({

                url: "/Admin/BreakingNews/Delete/" + id,
                type: "POST",

                success: function () {

                    showToast("success", "Ticker deleted.");

                    setTimeout(function () {

                        location.reload();

                    }, 800);

                },

                error: function () {

                    Swal.fire(
                        "Error",
                        "Unable to delete ticker.",
                        "error"
                    );

                }

            });

        });

    });

}

function wireActivateDeactivate() {

    $(document).on("click", ".activateBtn", function () {

        let id = $(this).data("id");

        let activate = $(this).text().trim() === "Activate";

        let url = activate
            ? "/Admin/BreakingNews/Activate/"
            : "/Admin/BreakingNews/Deactivate/";

        $.ajax({

            url: url + id,
            type: "POST",

            success: function () {

                showToast("success", "Ticker updated.");

                setTimeout(function () {

                    location.reload();

                }, 500);

            },

            error: function () {

                Swal.fire(
                    "Error",
                    "Unable to update ticker.",
                    "error"
                );

            }

        });

    });

}

function wireFilters() {

    $("#searchBox").on("keyup", function () {

        $("#breakingNewsTable")
            .DataTable()
            .search($(this).val())
            .draw();

    });

    $("#statusFilter").on("change", function () {

        $("#breakingNewsTable")
            .DataTable()
            .column(2)
            .search($(this).val())
            .draw();

    });

    $("#styleFilter").on("change", function () {

        $("#breakingNewsTable")
            .DataTable()
            .column(1)
            .search($(this).val())
            .draw();

    });

}

function showToast(icon, title) {

    Swal.fire({

        toast: true,
        position: "top-end",
        icon: icon,
        title: title,
        timer: 2000,
        showConfirmButton: false

    });

}