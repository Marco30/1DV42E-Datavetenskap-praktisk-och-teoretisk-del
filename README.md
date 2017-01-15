#Teroretisk och Pratiskt  

**för att den här API ska funka med kligenten jag byggt på cloud 9 så måste man ändra den hår kodade config.baseurl = "https://labb2-marco30.c9users.io" som finns i config/environments/development.rb till server adresen som du får i det cloud9 proejktet du använder för att köra APIen**


##Instruktioner 

gå till https://c9.io/ 
öpnna ny Ruby on Rails projekt 

Ladda upp filerna 

i rails cloud9 terminal-fönstret

Kör bundle install 

Kör db:migrate för databasen

Kör db:seed för att ladda data i databasen

starta servern i cloud9 och kopierar cloud9 server adressen för att använda det i postman

Innan du börjar använda postman så ska du gå till cloud9 terminalen, för att få din APIkey som generas automatiks 

rails c 

ApiKey.all

Kopierar APIKey för att använda det i postman

Visa saker i api kräver en autentiserar token, den får du genom att loga in i api,  det gör du genom postman filen som finns med 

öppna postman 

funktionerna i postman funkar så fort du lag till din cloud9 server adress, din APIKey och autentiserar token 






