using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using _2Korriku.Data;
using _2Korriku.Models;
namespace _2Korriku.Services;
public class StudentPhotoService(ApplicationDbContext db)
{
 public async Task Save(int id,IFormFile? photo,bool remove=false)
 {
  if(!await db.Students.AnyAsync(s=>s.Id==id))throw new UnauthorizedAccessException();
  byte[]? bytes=null;
  if(!remove){
   if(photo==null||photo.Length<30||photo.Length>204800)throw new ValidationException("Zgjidhni një foto. Madhësia optimizohet automatikisht.");
   using var stream=new MemoryStream();await photo.CopyToAsync(stream);bytes=stream.ToArray();
   Validate(bytes);
  }
  await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261027, 2031)");
  var row=await db.Set<StudentPhoto>().SingleOrDefaultAsync(p=>p.StudentId==id);
  if(remove){if(row!=null)db.Remove(row);}
  else {if(row==null){row=new(){StudentId=id};db.Add(row);}row.Data=bytes!;row.UpdatedAt=DateTime.UtcNow;}
  await db.SaveChangesAsync();await tx.CommitAsync();
 }
 // WebP RIFF chunks: https://developers.google.com/speed/webp/docs/riff_container
 public static void Validate(byte[] bytes)
 {
  void Invalid()=>throw new ValidationException("Fotoja nuk u përpunua. Zgjidhni përsëri JPG, PNG ose WebP.");
  if(bytes.Length<30||bytes.Length>204800||!bytes.AsSpan(0,4).SequenceEqual("RIFF"u8)||!bytes.AsSpan(8,4).SequenceEqual("WEBP"u8)||BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4,4))!=bytes.Length-8)Invalid();
  var offset=12;var frames=0;int? canvasWidth=null,canvasHeight=null;var width=0;var height=0;
  while(offset<bytes.Length){
   if(offset+8>bytes.Length)Invalid();
   var size=BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset+4,4));if(size>bytes.Length-offset-8)Invalid();
   var kind=System.Text.Encoding.ASCII.GetString(bytes,offset,4);var data=bytes.AsSpan(offset+8,(int)size);
   if(kind=="VP8X"){
    if(size!=10||offset!=12||(data[0]&2)!=0)Invalid();
    canvasWidth=1+data[4]+(data[5]<<8)+(data[6]<<16);canvasHeight=1+data[7]+(data[8]<<8)+(data[9]<<16);
   }else if(kind=="VP8 "){
    if(size<10||!data.Slice(3,3).SequenceEqual(new byte[]{0x9d,0x01,0x2a}))Invalid();
    width=BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6,2))&0x3fff;height=BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8,2))&0x3fff;frames++;
   }else if(kind=="VP8L"){
    if(size<5||data[0]!=0x2f)Invalid();var bits=BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(1,4));width=(int)(bits&0x3fff)+1;height=(int)((bits>>14)&0x3fff)+1;frames++;
   }else if(kind is not ("ICCP" or "ALPH"))Invalid();
   offset+=8+(int)size+((int)size&1);
  }
  if(offset!=bytes.Length||frames!=1||width is <1 or >640||height is <1 or >640||canvasWidth.HasValue&&(canvasWidth!=width||canvasHeight!=height))Invalid();
 }}
