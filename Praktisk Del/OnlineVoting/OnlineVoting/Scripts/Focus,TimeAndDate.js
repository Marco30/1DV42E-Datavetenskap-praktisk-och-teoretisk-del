"use strict";
// Marco villegas 
var DateAndTime =
    {
        start: function () {

            document.getElementById("Description").focus();// funktion som atomatisk fokuserar på en specifik textruta med id (Description)

            //$('#TimeEnd').timepicker({ 'timeFormat': 'H:i' });
            $("input[type='time']").timepicker({ 'timeFormat': 'H:i' });// funktion som aktiverar TimePicker, den körs på alla input HTML tagar med typ (Time) 

            $("input[type='date']").datepicker({
                dateFormat: "yy-mm-dd",
            }); // funktion som aktiverar DatePicker, den körs på alla input HTML tagar med typ (Date) 
        },

    };

window.onload = DateAndTime.start;// startar funktionen som har label start när sidan har ladats