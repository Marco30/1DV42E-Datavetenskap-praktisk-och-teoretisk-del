"use strict";
// Marco villegas 
var DateAndTime =
    {
        start: function () {

            var IDnames = new Array("FixEditFocus", "Description", "Descripcion", "UserName", "FirstName", "OldPassword");// använda för att kuna hitta ID i HTML systemet

            for (var i = 0; i < IDnames.length; i++)
            {
                
                var ID = document.getElementById(IDnames[i])// hittar ID i HTML

                //console.error(IDnames[i]);
                if (ID !== null && ID !== 'undefined')// kontrollerar att ID finns, om ID inte finns så körs if satsen som i sintur markerar en textruta för att underläta för användare 
                {
                    if (IDnames[i] === "FixEditFocus")
                    {
                        document.getElementById("FirstName").focus();
                        break;
                    }
                    document.getElementById(IDnames[i]).focus();
                    break;
                }
         
            }

            //$('#TimeEnd').timepicker({ 'timeFormat': 'H:i' });
            $("input[type='time']").timepicker({ 'timeFormat': 'H:i' });// funktion som aktiverar TimePicker, den körs på alla input HTML tagar med typ (Time) 

            $("input[type='date']").datepicker({
                dateFormat: "yy-mm-dd",
            }); // funktion som aktiverar DatePicker, den körs på alla input HTML tagar med typ (Date) 

        },





    };

window.onload = DateAndTime.start;// startar funktionen som har label start när sidan har ladats