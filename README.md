# PulseTune 🧠

PulseTune, Windows sistemleri için gelişmiş bir sistem optimizasyon ve izleme uygulamasıdır. Bu uygulama sistem kaynaklarınızı gerçek zamanlı takip eder, performans sorunlarını tespit eder ve sisteminizi optimize etmenize yardımcı olur.

## Özellikler

- **Sistem İzleme**
  - CPU, RAM, Disk ve Ağ kullanımı gerçek zamanlı takibi
  - Kaynak kullanımı grafikleri
  - Çalışan işlemlerin detaylı incelenmesi

- **Başlangıç Programları Yönetimi**
  - Sistemle birlikte çalışan programların listelenmesi
  - Başlangıç öğelerini etkinleştirme/devre dışı bırakma
  - Başlangıç öğelerinin sistem üzerindeki etkilerinin analizi

- **Servis Yönetimi**
  - Windows servislerinin incelenmesi ve kontrolü
  - Gereksiz servislerin tespiti ve optimizasyon önerileri

- **Profil Sistemi**
  - Farklı kullanım senaryoları için optimize edilmiş profiller
  - Gaming, Ofis, Enerji Tasarrufu ve Normal profilleri
  - Profilinize uygun sistem ayarları otomatik uygulama

- **Hızlı Tarama ve Optimizasyon Önerileri**
  - Tek tıkla sistem analizi
  - Belirlenmiş performans sorunları için özelleştirilmiş çözüm önerileri

## Sistem Gereksinimleri

- Windows 10/11
- .NET 9.0 runtime
- En az 2GB RAM
- Administrator hakları (bazı özellikler için)

## Kurulum

1. Releases bölümünden son sürümü indirin
2. Kurulum dosyasını çalıştırın ve adımları takip edin
3. Kurulum tamamlandığında PulseTune'u başlatın

## Nasıl Kullanılır

1. Ana ekrandaki grafiklerden sistem kaynaklarının durumunu takip edin
2. "İşlemler" sekmesinden yüksek kaynak kullanan uygulamaları görüntüleyin
3. "Başlangıç" sekmesinde başlangıç programlarını yönetin
4. "Servisler" sekmesinde sistem servislerini kontrol edin
5. "Öneriler" sekmesinde sistem için otomatik öneriler alın
6. Üstteki profil seçici ile kullanım senaryonuza uygun profil seçin

## Geliştirme

Proje .NET platformu üzerinde WPF ile geliştirilmiştir. Projeyi geliştirmeye katkıda bulunmak için:

```bash
# Depoyu klonlayın
git clone https://github.com/kullaniciadiniz/PulseTune.git

# Proje dizinine girin
cd PulseTune

# Bağımlılıkları yükleyin
dotnet restore

# Projeyi çalıştırın
dotnet run
```

## Lisans

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.
