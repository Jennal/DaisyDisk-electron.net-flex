$(() => {
    // $("#app").html("Hello");
    window.vm = new Vue({
        el: "#app",
        data: {
            path: "",
            progress: 0,
            pie: {}
        },
        watch: {
            pie: function(newVal, oldVal) {
                console.log("pie updated", newVal);
            }
        },
        methods: {
            selectDir: async function() {
                var path = await client.invokeNode("openFolder()");
                console.log(path);
                if (!path || !path.length) return;
                
                client.invoke("ElectronFlex.Handler", "Create", path[0]);
            }
        }
    });
});