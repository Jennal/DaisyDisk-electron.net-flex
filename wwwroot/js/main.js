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
            selectDir: async function() {
                var path = await client.invokeNode("openFolder()");
                console.log(path);
            }
        }
    });
});