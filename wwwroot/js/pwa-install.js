window.pwaInstall = {

    deferredPrompt: null,

    init: function () {

        window.addEventListener("beforeinstallprompt", (event) => {

            // Prevent Chrome from showing its own install prompt
            event.preventDefault();

            // Save the event for later
            this.deferredPrompt = event;

            // Show our own button
            this.showButton();

        });


        window.addEventListener("appinstalled", () => {

            console.log("MyBauManagement wurde installiert.");

            this.deferredPrompt = null;

            this.hideButton();

        });


        // If already running as installed PWA
        if (window.matchMedia("(display-mode: standalone)").matches) {

            this.hideButton();

        }

    },


    install: async function () {

        if (!this.deferredPrompt) {

            console.log(
                "Keine PWA-Installation verfügbar."
            );

            return;

        }


        const promptEvent = this.deferredPrompt;

        this.deferredPrompt = null;

        await promptEvent.prompt();


        const result =
            await promptEvent.userChoice;


        console.log(
            "PWA installation:",
            result.outcome
        );


        this.hideButton();

    },


    showButton: function () {

        const button =
            document.getElementById("installAppBtn");

        if (button) {

            button.style.display = "flex";

        }

    },


    hideButton: function () {

        const button =
            document.getElementById("installAppBtn");

        if (button) {

            button.style.display = "none";

        }

    }

};