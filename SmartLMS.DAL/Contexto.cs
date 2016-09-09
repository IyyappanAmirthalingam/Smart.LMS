﻿using Carubbi.GenericRepository;
using Carubbi.Utils.Persistence;
using SmartLMS.DAL.Mapeamento;
using SmartLMS.Dominio;
using SmartLMS.Dominio.Entidades;
using SmartLMS.Dominio.Entidades.Historico;
using SmartLMS.Dominio.Entidades.Pessoa;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace SmartLMS.DAL
{
    public class Contexto : DbContext, IContexto
    {

        public Contexto()
              : base("name=StringConexao")
        {
        }

        private Dictionary<Type, object> _dicionarioSerializadores = new Dictionary<Type, object>();

        private Serializer<TEntidade> ObterSerializador<TEntidade>()
        {
            Type tipoEntidade = typeof(TEntidade);
            if (!_dicionarioSerializadores.ContainsKey(tipoEntidade))
            {
                _dicionarioSerializadores.Add(tipoEntidade, new Serializer<TEntidade>());
            }

            return (Serializer<TEntidade>)_dicionarioSerializadores[tipoEntidade];
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();


            modelBuilder.Entity<Parametro>().Property(o => o.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            modelBuilder.Entity<Log>().Property(o => o.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Configurations.Add(new AvisoConfiguration());
            modelBuilder.Configurations.Add(new UsuarioConfiguration());
            modelBuilder.Configurations.Add(new AlunoConfiguration());
            modelBuilder.Configurations.Add(new AreaConhecimentoConfiguration());
            modelBuilder.Configurations.Add(new AssuntoConfiguration());
            modelBuilder.Configurations.Add(new CursoConfiguration());
            modelBuilder.Configurations.Add(new AulaConfiguration());
            modelBuilder.Configurations.Add(new AcessoAulaConfiguration());
            modelBuilder.Configurations.Add(new ArquivoConfiguration());
            modelBuilder.Configurations.Add(new AcessoArquivoConfiguration());
            modelBuilder.Configurations.Add(new TurmaConfiguration());
            modelBuilder.Configurations.Add(new TurmaCursoConfiguration());
            modelBuilder.Configurations.Add(new AulaPlanejamentoConfiguration());
            modelBuilder.Configurations.Add(new UsuarioAvisoConfiguration());
        }

        public IDbSet<TEntidade> ObterLista<TEntidade>() where TEntidade : class
        {
            return base.Set<TEntidade>();
        }

        public void Atualizar<TEntidade>(TEntidade objetoAntigo, TEntidade objetoNovo) where TEntidade : class
        {
            Entry(objetoAntigo).CurrentValues.SetValues(objetoNovo);
            Entry(objetoAntigo).State = EntityState.Modified;
        }

        public void Salvar()
        {
            Salvar(ObterVisitante());
        }

        public void Salvar(Usuario usuarioLogado)
        {
            DateTime agora = DateTime.Now;
            /*   if (usuarioLogado != null)
               {
                   foreach (var entry in ChangeTracker?.Entries()?.Where(x => x.Entity?.GetType() != typeof(Log)))
                   {
                       GerarLog(entry, usuarioLogado);
                   }
               }*/
            SaveChanges();
        }

        private void GerarLog(DbEntityEntry entry, Usuario usuarioLogado)
        {
            var serializador = this
                          .GetType()
                          .GetMethod("ObterSerializador")
                          .MakeGenericMethod(entry.Entity.GetType())
                          .Invoke(this, null);

            var tipoSerializador = serializador.GetType();

            var metodoSerializar = tipoSerializador
              .GetMethod("XmlSerialize");


            var xmlAntigo = string.Empty;
            var xmlNovo = string.Empty;

            if (entry.State == (EntityState.Deleted | EntityState.Modified))
            {
                xmlAntigo = metodoSerializar
                  .Invoke(serializador, new object[] { entry.OriginalValues }).ToString();
            }

            if (entry.State == (EntityState.Added | EntityState.Modified))
            {
                xmlNovo = metodoSerializar
                  .Invoke(serializador, new object[] { entry.CurrentValues }).ToString();
            }

            if (!string.IsNullOrWhiteSpace(xmlAntigo) || !string.IsNullOrWhiteSpace(xmlNovo))
            {
                Entry(new Log
                {
                    EstadoAntigo = xmlAntigo.ToString(),
                    EstadoNovo = xmlNovo.ToString(),
                    DataHora = DateTime.Now,
                    Usuario = usuarioLogado,
                    IdEntitdade = ((Entidade)entry.Entity).Id,
                    Tipo = entry.Entity.GetType().ToString()
                }).State = EntityState.Added;
            }
        }

        private Usuario ObterVisitante()
        {
            return Set<Usuario>().SingleOrDefault(u => u.Login == Usuario.AGENTE_INTERNO);
        }

        IDbSet<T> IDbContext.Set<T>()
        {
            return ObterLista<T>();
        }

        public void ConfigurarParaApi()
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public T UnProxy<T>(T proxyObject) where T : class
        {
            var proxyCreationEnabled = Configuration.ProxyCreationEnabled;
            try
            {
                Configuration.ProxyCreationEnabled = false;
                T poco = Entry(proxyObject).CurrentValues.ToObject() as T;
                return poco;
            }
            finally
            {
                Configuration.ProxyCreationEnabled = proxyCreationEnabled;
            }
        }

        public void Recarregar<T>(T entidade) where T : class
        {
            Entry<T>(entidade).Reload();
        }

    }
}
