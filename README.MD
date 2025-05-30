# Setup

`./setup.sh`

# How To Use

`dotnet run 'link'`

for example to scrape [this](https://milliebobbybrown.com.br/galeria/thumbnails.php?album=654https:/) you would run `dotnet run https://milliebobbybrown.com.br/galeria/thumbnails.php?album=654`

else if (Listal_ComScraper.IsListalUrl(link))
await Listal_ComScraper.Download(link, downloadedMediaFolder);

# Supported Sites

* Website's that use Coppermine Photo Gallery (https://milliebobbybrown.com.br/, https://kendalljenner.com.br/, etc..)
* sophialillisfan.com
* haileesteinfield.com
* sadiesinkfan.com
* Will run generic image scrape if the site is not supported
