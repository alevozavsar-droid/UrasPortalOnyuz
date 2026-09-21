// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using WebApplication3.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication3.Repositories
{

    public interface IFiloRepository
    {



        Task<IEnumerable<Arac>> GetAllAraclarAsync();
        Task<Arac> GetAracByIdAsync(int id);
        Task AddAracAsync(Arac arac);
        Task UpdateAracAsync(Arac arac);
        Task DeleteAracAsync(int id);




        Task<IEnumerable<Personel>> GetAllPersonelAsync();
        Task<Personel> GetPersonelByIdAsync(int id);
        Task AddPersonelAsync(Personel personel);
        Task UpdatePersonelAsync(Personel personel);
        Task DeletePersonelAsync(int id);




        Task<IEnumerable<AracAtama>> GetAktifAtamalarAsync();
        Task AddAtamaAsync(AracAtama atama);
        Task<AracAtama> GetAktifAtamaByAracIdAsync(int aracId);
        Task SonlandirAtamaAsync(AracAtama atama);
    }
}