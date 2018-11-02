// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
(function () {
    let body = document.getElementsByTagName("body");
    if (body && body.length == 1) {

        function removeButtons(fromElement) {
            let buttons = fromElement.children;
            for (let i = buttons.length - 1; i >= 0; i--) {
                if (buttons[i].classList.contains("view") || buttons[i].classList.contains("send") || buttons[i].classList.contains("delete")) {
                    fromElement.removeChild(buttons[i]);
                }
            }
        }

        body[0].addEventListener("click", function (e) {
            if (e.target.classList.contains("delete")) {
                e.preventDefault();
                let blob = e.target.getAttribute("data-blob");

                fetch('/api/DeleteBlob?blob=' + encodeURIComponent(blob), { method: "DELETE" })
                    .then(function (response) {
                        if (response.ok) {
                            if (e.target.parentElement.classList.contains("actions")) {
                                e.target.parentElement.parentElement.classList.add("deleted");
                            }
                            else {
                                e.target.parentElement.classList.add("deleted");
                            }
                            removeButtons(e.target.parentElement);
                            console.log("Deleted blob " + blob);
                            return;
                        }
                        throw new Error('Network response was not ok.');
                    })
                    .catch(function (error) {
                        e.target.parentElement.parentElement.className = "error";
                        console.log('There was a problem deleting blob ' + blob + ': ', error.message);
                    });

            }

            else if (e.target.classList.contains("send")) {
                e.preventDefault();
                let blob = e.target.getAttribute("data-blob");

                fetch('/api/SendEmail?blob=' + encodeURIComponent(blob), { method: "POST" })
                    .then(function (response) {
                        if (response.ok) {
                            if (e.target.parentElement.classList.contains("actions")) {
                                e.target.parentElement.parentElement.classList.add("sent");
                            }
                            else {
                                e.target.parentElement.classList.add("sent");
                            }
                            removeButtons(e.target.parentElement);
                            console.log("Sent email from blob " + blob);
                            return;
                        }
                        throw new Error('Network response was not ok.');
                    })
                    .catch(function (error) {
                        if (e.target.parentElement.classList.contains("actions")) {
                            e.target.parentElement.parentElement.classList.add("error");
                        }
                        else {
                            e.target.parentElement.classList.add("error");
                        }
                        console.log('There was a problem sending email from blob ' + blob + ': ', error.message);
                    });

            }

            else if (e.target.classList.contains("send-all")) {
                e.preventDefault();
                let sendButtons = document.getElementsByClassName("send");
                for (let i = 0; i < sendButtons.length; i++) {
                    sendButtons[i].click();
                }
            }
        })
    }
})();