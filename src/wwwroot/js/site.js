window.onload = function () {
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
                    icon = toggler.querySelector('.input__icon');

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