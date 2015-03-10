ASP.NET MVC REST Extensions
===========================

Problem Definition
------------------
1. Too many DTOs (Data Transfer Objects) combinations based on different user roles and different semantics.
2. Total number of methods = Number of user roles * Number of entities.
3. Trying to write base classes to extract common functionalities found in problem defition #2.
4. Finally repeating business logic in each of methods as well.


Goal
----
Our goal was to reduce number of methods, write one and only one simple business logic per entity 
and create a database "Firewall" referred as "SecurityContext" which allows specifiying access rules in the 
form of LINQ and you can also load them dynamically from database.

Database Firewall?
------------------
Let's review Firewall rules.

     INCOMING PORT 80 ALLOW
     INCOMING PORT 443 ALLOW
     INCOMING PORT * DISALLOW

Security Context
----------------
How about similar firewall rules for Entity Framework based on current user that is logged in.

     // assuming db.UserID has value of current logged in User

     public class UserSecurityContext : BaseSecurityContext<CompanyDbContext>{
     
          // You should only use singleton instance
          // for performance reasons
          public static UserSecurityContext Instance = new UserSecurityContext();
     
          private UserSecurityContext(){
              SetWebUserRule(CreateRule<WebUser>());
              SetMessageRule(CreateRule<Message>());
          }

          private void SetWebUserRule(
              EntityPropertyRulesCreator<WebUser,CompanyDbContext> rule){

              // user cannot delete anything from this table
              rule.SetDeleteRule(rule.NotSupportedRule);

              // user can read only record that has UserID set to current UserID
              rule.SetReadRule(db => 
                  user => user.UserID == db.UserID);

              // user can read/write only his/her email address and name
              rule.SetProperty(SerializeMode.ReadWrite,
                  user => user.FirstName,
                  user => user.LastName,
                  user => user.EmailAddress);

              // user can read his membership status
              rule.SetProperty(SerializeMode.Read,
                  user => user.MembershipStatus);

          }

          private void SetMessageRule(
              EntityPropertyRulesCreator<Message,CompanyDbContext> rule){

              // user cannot delete anything from this table
              rule.SetDeleteRule(rule.NotSupportedRule);

              // user can read messages that were sent by/to him/her
              rule.SetReadRule(db => 
                  msg => msg.UserID == db.UserID || msg.Recipients.Any( rcpt => rcpt.UserID == db.UserID ));

              // user can write messages that were sent by him/her
              rule.SetWriteRule(db => 
                  msg => msg.UserID == db.UserID);

              // user can read/write only his/her email address and name
              rule.SetProperty(SerializeMode.ReadWrite,
                  user => user.Subject,
                  user => user.Message);

          }

     }

Usage
-----
      using(ModelDbContext db = new ModelDbContext()){
          
          // db.SecurityContext is null by default
          // all operations are executed without any security
          
          db.SecurityContext = UserSecurityContext.Instance;
          // set current user id
          db.UserID = 2;
          
          // security context rules are automatically applied here
          var user = db.Query<WebUser>().FirstOrDefault();
          // try to modify password
          user.Password = "password";
          
          // raises an exception saying password cannot be modified
          db.SaveChanges();
      }

How does it work?
-----------------
Instead of DbContext, we have provided AtomDbContext, which contains SecurityContext property, 
and all operations performed through this class passed through SecurityContext and it obeys every rule.

DbContext Generator
-------------------

We have included text template in the folder "db-context-tt", which you have to include in your Model folder and set
connection string to generate DbContext model automatically from specified database.

Query Method
------------
If you use `db.Messages.Where()` method, there are no security rules applied. So you have to use following Query 
method. Instead you must use `db.Query<Message>()`.

SaveChanges Method
------------------

When you call SaveChanges method, following logic is executed.

This is for reference, this happens automatically as a part of validation logic, you do not have to 
    write this code.
    
    //For every modified entity, we query entity to database with identity query for example,


    void VerifyWriteAccess(entity entityToModify){
        var editedEntity = entityToModify;
        var q =  db.Query<Message>();

        // security rule
        q = q.Where(msg => msg.UserID == db.UserID);

        var dbEntity = q = q.FirstOrDefault(msg => msg.MessageID == editedEntity.MessageID);

        // if for any reason security rule failed, dbEntity will be null
        if(dbEntity == null)
             throw new EntityAccessException("You do not have access to Entity Message");

        // verify if current SecurityContext has write access to 
        // to each of modified property
    }

    // This is inside a transaction

    // VERIFY WRITE ACCESS
    // Before running SaveChanges

    VerifyWriteAccess(entityToModify);

    // Perform Save Action to database
    base.SaveChanges();

    // VERIFY WRITE ACCESS
    // after running SaveChanges
    // Exception here causes transaction to rollback
    VerifyWriteAccess(entityToModify);


Entity Controller for Mvc
=========================

Setup Route
-----------

            context.MapRoute(
                "App_entity",
                "App/Entity/{table}/{action}",
                new { controller = "Entity", action = "Query" },
                new string[] { "MyApp.Areas.App.Controllers" }
            );

Query Method Example
--------------------
Return all messages sent by user with id 2, with DateSent in descending order.    


      /app/entity/message/query
            ?query={UserID:2}
            &orderBy=DateReceived+DESC
            &fields={MessageID:'',Subject:''}
            &start=10
            &size=10

      query expects anonymous object as filter, here are more examples

Filter Operators
----------------
Filtering was made easy to read and easy to create from JavaScript.

      Messages with UserID more than 2
      {'UserID >': 2}

Navigation Property Filter
--------------------------
      Messages sent to UserID 2
      {'Recepients any': { UserID: 2 }}

This one is tricky, let's review Message Model, basically Message
class has Recepients navigation property so query

      {'Recepients any': { UserID: 2 }} 
    
Translates to following linq

      msg => msg.Recepients.Any( rcpt => rcpt.UserID == 2 );

Example Queries
---------------

     { 'Parent.UserID':2 }
     msg => msg.Parent.UserID == 2;

     { 'UserID between': [3,7]}
     msg => msg.UserID <=3 && msg.UserID >= 7;

     // by default, multiple conditions are combined with "AND" operand
     { UserID: 4, 'Status !=': 'Sent' }
     msg => msg.UserID = 4 && msg.Status != 'Sent'

     // for OR, condition group must be enclosed with $or as follow
     { $or: { 'UserID !=':4, Status: 'Sent'  } }
     msg => msg.UserID != 4 || msg.Status == 'Sent') 

     { UserID: 4 , $or: { 'UserID !=':4, Status: 'Sent'  } }
     msg => msg.UserID == 4 && ( msg.UserID != 4 || msg.Status == 'Sent') 

     { 'Status in':[ 'Sent', 'Pending' ] }
     var list = new List<string>(){ 'Sent','Pending' };
     msg => list.Contains(msg.Status);

Each of query is filtered upon existing Security Context rules, and 
properties are returned only on the basis of Read access, so you do not have to
write or worry about any DTOs anymore.
