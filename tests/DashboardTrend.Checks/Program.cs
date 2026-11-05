using _2Korriku.Services;
static void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS: "+message);}
var today = new DateOnly(2026,1,5);
var result = RegistrationTrend.Build([
    BillingClock.UtcDate(new(2025,7,31)),
    BillingClock.UtcDate(new(2025,8,1)),
    BillingClock.UtcDate(new(2025,12,31)),
    new DateTime(2025,12,31,23,30,0,DateTimeKind.Utc),
    BillingClock.UtcDate(today),
    BillingClock.UtcDate(today.AddDays(1))
],today);
Check(result.Months.Count==6 && result.Months[0].Month==new DateOnly(2025,8,1),"Rolling six months cross year boundary");
Check(result.Total==4,"Old and future registrations excluded");
Check(result.CurrentMonth==2,"Registration buckets respect local midnight");
Check(result.Months[1].Count==0,"Missing months are zero-filled");
Check(result.Months[4].Count==1,"December retained separately from January");
var empty=RegistrationTrend.Build([],today);
Check(empty.Total==0&&empty.CurrentMonth==0&&empty.Months.Count==6,"Empty clubs have six zero months");
var dst = RegistrationTrend.Build([new DateTime(2026,3,31,22,30,0,DateTimeKind.Utc)],new(2026,4,1));
Check(dst.CurrentMonth==1,"Summer time month boundary handled");
