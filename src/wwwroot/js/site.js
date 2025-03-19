window.onload = function () {

    //document.addEventListener('DOMContentLoaded', function () {
    //    const inputDate = document.querySelector('.input-date');
    //    const selectInputs = document.querySelectorAll('.input-select');

    //    //const today = new Date();
    //    //const formattedDate = today.toISOString().split('T')[0];
    //    //inputDate.value = formattedDate;
    //    inputDate.value = Date.now;

    //    // Установка первого пункта в выпадающих списках
    //    selectInputs.forEach(select => {
    //        if (select.options.length > 1) {
    //            select.selectedIndex = 1;
    //        }
    //    });
    //});
    function Copy() {
        var Url = document.createElement("textarea");
        Url.innerHTML = window.location.href;
        Copied = Url.createTextRange();
        Copied.execCommand("Copy");
    }
    (function () {
        const inputText = document.querySelectorAll('.form__input');

        inputText.forEach(function (input) {
            if (input.value) {
                input.classList.add('focus');
                input.parentElement.querySelector('.input-placeholder').classList.add('focus');
            }
            input.addEventListener('focus', function () {
                this.classList.add('focus');
                this.parentElement.querySelector('.input-placeholder').classList.add('focus');
            });
            input.addEventListener('blur', function () {
                this.classList.remove('focus');
                if (!this.value) {
                    this.parentElement.querySelector('.input-placeholder').classList.remove('focus');
                }
            });
        });
    })();


    (function () {
        const togglers = document.querySelectorAll('.password-toggler');

        togglers.forEach(function (checkbox) {
            checkbox.addEventListener('change', function () {

                const toggler = this.parentElement,
                    input = toggler.parentElement.querySelector('.input-password'),
                    icon = toggler.querySelector('.auth-form__icon');

                if (checkbox.checked) {
                    input.type = 'text';
                    icon.classList.remove('la-eye')
                    icon.classList.add('la-eye-slash');
                }

                else {
                    input.type = 'password';
                    icon.classList.remove('la-eye-slash')
                    icon.classList.add('la-eye');
                }
            });
        });
    })();
};