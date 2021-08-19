$(() => {
    // $("#app").html("Hello");
    window.vm = new Vue({
        el: "#app",
        data: {
            path: "",
            progress: 0,
            pie: null
        },
        methods: {
            selectDir: function() {
                alert("select dir");
            }
        }
    });
});