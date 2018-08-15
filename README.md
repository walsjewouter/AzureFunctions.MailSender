# AzureFunctions.MailSender

I have some web applications running on Azure and these applications send out some emails to users.
The dynamic parts of the messages are store in Azure table storage and are parse by Razor used a model.
To complete the entire email body, an HTMl file is loaded and parts are replaced with the Razor output.
Then is is send to an email service, which takes care of the delivery.

I was experiencing performance issues, each time an email was being send.
Thats why I wanted to use an Azure Function to handle the processing of emails en sending it to the email service.

Not my web applications only have to deliver a message into a queue, to trigger the function, which should be a big performance improvement.

Currenlty I only have this solution build and it is running on Azure, not I need to update the existing mail service of the web applications, in order to make is push a message into the queue, instead of actually rendering and sending the mail.

* The MailSender project is the actual solution.
* The Trigger project is a small console application for local testing.
