using System.ComponentModel.DataAnnotations;
using _2Korriku.Models;
using _2Korriku.Services;

static void Reject(PlayerContract contract,DateOnly start,DateOnly end,string label)
{
    try{PlayerContractService.ValidateDates(contract,start,end);}
    catch(ValidationException){Console.WriteLine("PASS: "+label);return;}
    throw new Exception(label);
}
var contract=new PlayerContract();
PlayerContractService.ValidateDates(contract,new(2026,10,30),new(2027,10,29));
PlayerContractService.ValidateDates(contract,new(2026,10,30),new(2026,10,30));
Reject(contract,new(2027,1,1),new(2026,1,1),"Reversed dates rejected");
Reject(contract,new(1899,1,1),new(2026,1,1),"Unsupported start year rejected");
Reject(contract,new(2026,1,1),new(2201,1,1),"Unsupported end year rejected");
contract.Annexes.Add(new(){EffectiveOn=new(2026,12,1),Status="Active"});
Reject(contract,new(2027,1,1),new(2027,12,1),"Start cannot exclude an annex");
Reject(contract,new(2026,1,1),new(2026,11,30),"Expiry cannot exclude an annex");
PlayerContractService.ValidateDates(contract,new(2026,12,1),new(2026,12,1));
contract.EndedOn=new(2026,12,15);
Reject(contract,new(2026,1,1),new(2026,12,14),"Expiry cannot exclude termination date");
Reject(contract,new(2026,12,16),new(2027,1,1),"Start cannot follow termination date");
PlayerContractService.ValidateDates(contract,new(2026,1,1),new(2027,1,1));
Console.WriteLine("PASS: Valid dates, inclusive annex boundaries and terminated contracts");
