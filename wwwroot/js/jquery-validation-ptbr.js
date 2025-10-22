// Arquivo: wwwroot/js/jquery-validation-ptbr.js
(function ($) {
    // Sobrescreve o validador 'number' do jQuery para aceitar o formato pt-BR (com vírgula)
    $.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?(?:\d+|\d{1,3}(?:.\d{3})*)(?:,\d+)?$/.test(value);
    };

    // Sobrescreve o validador 'range' para tratar a vírgula
    $.validator.methods.range = function (value, element, param) {
        if (this.optional(element)) {
            return true;
        }
        var valueWithDot = value.replace(/\./g, "").replace(",", ".");
        var val = parseFloat(valueWithDot);
        var min = parseFloat(String(param[0]).replace(",", "."));
        var max = parseFloat(String(param[1]).replace(",", "."));
        return !isNaN(val) && val >= min && val <= max;
    };
}(jQuery));