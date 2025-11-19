using System;
using System.IO;
using System.Collections.Generic;
using AutoMarket.Models;

namespace AutoMarket.Data
{
    public static class ImageSeeder
    {
        public static void Seed(AppDbContext context)
        {
            var basePath = Path.Combine("wwwroot", "img", "Anuncios");

            var marcas = new Dictionary<string, int>
            {
                { "BMW", 1 },
                { "Audi", 2 },
                { "Mercedes", 3 },
                { "Volkswagen", 4 },
                { "Mazda", 5 },
                { "Kia", 6 },
                { "Seat", 7 },
                { "Tesla", 8 },
                { "Jaguar", 9 }
            };

            foreach (var marca in marcas)
            {
                var pasta = Path.Combine(basePath, marca.Key);
                if (!Directory.Exists(pasta))
                    continue;

                var ficheiros = Directory.GetFiles(pasta);

                int ordem = 1;
                foreach (var file in ficheiros)
                {
                    var imagem = new Imagem
                    {
                        AnuncioId = marca.Value,
                        UrlImagem = $"/img/Anuncios/{marca.Key}/{Path.GetFileName(file)}",
                        Ordem = ordem++
                    };

                    context.Imagens.Add(imagem);
                }
            }

            context.SaveChanges();
        }
    }
}
