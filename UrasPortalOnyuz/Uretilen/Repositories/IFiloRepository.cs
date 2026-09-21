// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using WebApplication3.Data;
using WebApplication3.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Repositories;
namespace WebApplication3.Repositories
{

    public class FiloRepository : IFiloRepository
    {




        public async Task<IEnumerable<Arac>> GetAllAraclarAsync()  {return default;
}

        public async Task<Arac> GetAracByIdAsync(int id)  {return default;
}

        public async Task AddAracAsync(Arac arac)
 {}

        public async Task UpdateAracAsync(Arac arac)
 {}


        public async Task DeleteAracAsync(int id)
 {}




        public async Task<IEnumerable<Personel>> GetAllPersonelAsync()  {return default;
}

        public async Task<Personel> GetPersonelByIdAsync(int id)  {return default;
}

        public async Task AddPersonelAsync(Personel personel)
 {}

        public async Task UpdatePersonelAsync(Personel personel)
 {}


        public async Task DeletePersonelAsync(int id)
 {}





        public async Task<IEnumerable<AracAtama>> GetAktifAtamalarAsync()  {return default;
}

        public async Task AddAtamaAsync(AracAtama atama)
 {}

        public async Task<AracAtama> GetAktifAtamaByAracIdAsync(int aracId)  {return default;
}

        public async Task SonlandirAtamaAsync(AracAtama atama)
 {}
    }
}