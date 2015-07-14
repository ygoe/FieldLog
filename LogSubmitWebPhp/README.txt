FieldLog – .NET logging solution
© Yves Goergen, Made in Germany
Licence: GNU GPL v3
Website: http://unclassified.software/source/fieldlog

This directory contains the FieldLog submit web API written in PHP.

You can run your own submit webservice with this script.

Copy the file logsubmit.php to your web server. You can rename the file. Use the script in a web
browser to generate a new API token and edit the configuration sections in the script file
accordingly. Use the script's public URL and also include the generated token in your submit.conf
file deployed with LogSubmit.exe. Example:

transport.http.url = https://example.com/api/fieldlog/logsubmit.php
transport.http.token = 01234567890123456789

Alternatively, you can register with my instance of the script, running on the built-in default
URL. Send me an e-mail or fill out the contact form on the project website to request registration
to obtain your individual API token. My service is available with SSL encryption and supports a
maximum file upload size of 150 MiB.
