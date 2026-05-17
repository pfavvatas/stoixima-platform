-- Seed: Football teams — Top 5 European Leagues
-- Safe to run multiple times (ON CONFLICT DO NOTHING on name+league)
-- Aliases are used by MessageProcessing service for fuzzy team recognition.

-- ─── Premier League ───────────────────────────────────────────────────────────

INSERT INTO teams (name, aliases, country, league) VALUES
('Arsenal',                  ARRAY['Arsenal FC','The Gunners','AFC'],                            'England', 'Premier League'),
('Aston Villa',              ARRAY['Villa','AVFC','Aston Villa FC'],                             'England', 'Premier League'),
('Bournemouth',              ARRAY['AFC Bournemouth','The Cherries','Bournemouth FC'],           'England', 'Premier League'),
('Brentford',                ARRAY['Brentford FC','The Bees'],                                   'England', 'Premier League'),
('Brighton',                 ARRAY['Brighton & Hove Albion','Brighton and Hove Albion','Seagulls','BHAFC'], 'England', 'Premier League'),
('Chelsea',                  ARRAY['Chelsea FC','The Blues','CFC'],                              'England', 'Premier League'),
('Crystal Palace',           ARRAY['Palace','The Eagles','CPFC','Crystal Palace FC'],           'England', 'Premier League'),
('Everton',                  ARRAY['Everton FC','The Toffees','EFC'],                           'England', 'Premier League'),
('Fulham',                   ARRAY['Fulham FC','The Cottagers'],                                 'England', 'Premier League'),
('Ipswich Town',             ARRAY['Ipswich','Town','Ipswich City'],                            'England', 'Premier League'),
('Leicester City',           ARRAY['Leicester','The Foxes','LCFC','Leicester City FC'],         'England', 'Premier League'),
('Liverpool',                ARRAY['Liverpool FC','The Reds','LFC'],                            'England', 'Premier League'),
('Manchester City',          ARRAY['Man City','City','MCFC','Man. City'],                       'England', 'Premier League'),
('Manchester United',        ARRAY['Man Utd','Man United','MUFC','The Red Devils','Man. United'], 'England', 'Premier League'),
('Newcastle United',         ARRAY['Newcastle','NUFC','The Magpies','Newcastle Utd'],           'England', 'Premier League'),
('Nottingham Forest',        ARRAY['Forest','NFFC','The Reds','Notts Forest'],                  'England', 'Premier League'),
('Southampton',              ARRAY['Southampton FC','The Saints','Saints'],                      'England', 'Premier League'),
('Tottenham Hotspur',        ARRAY['Spurs','Tottenham','THFC','Tottenham Hotspur FC'],          'England', 'Premier League'),
('West Ham United',          ARRAY['West Ham','The Hammers','WHU','West Ham Utd'],              'England', 'Premier League'),
('Wolverhampton Wanderers',  ARRAY['Wolves','WWFC','Wolverhampton','Wolves FC'],                'England', 'Premier League')
ON CONFLICT DO NOTHING;

-- ─── La Liga ─────────────────────────────────────────────────────────────────

INSERT INTO teams (name, aliases, country, league) VALUES
('Athletic Club',       ARRAY['Athletic Bilbao','Athletic','Club Atletico'],                    'Spain', 'La Liga'),
('Atletico Madrid',     ARRAY['Atletico','Atlético Madrid','Atletico de Madrid','Atléti','ATM'], 'Spain', 'La Liga'),
('Barcelona',           ARRAY['Barca','FC Barcelona','FCB','Barça'],                            'Spain', 'La Liga'),
('Real Betis',          ARRAY['Betis','Betis Balompie','Real Betis Balompie'],                  'Spain', 'La Liga'),
('Celta Vigo',          ARRAY['Celta','RC Celta','RC Celta de Vigo'],                           'Spain', 'La Liga'),
('Deportivo Alaves',    ARRAY['Alaves','Alavés','Deportivo Alavés'],                            'Spain', 'La Liga'),
('Espanyol',            ARRAY['RCD Espanyol','Espanyol FC'],                                    'Spain', 'La Liga'),
('Getafe',              ARRAY['Getafe CF'],                                                     'Spain', 'La Liga'),
('Girona',              ARRAY['Girona FC'],                                                     'Spain', 'La Liga'),
('Las Palmas',          ARRAY['UD Las Palmas','Las Palmas FC'],                                 'Spain', 'La Liga'),
('Leganes',             ARRAY['CD Leganes','Leganés'],                                          'Spain', 'La Liga'),
('Mallorca',            ARRAY['RCD Mallorca','Mallorca FC'],                                    'Spain', 'La Liga'),
('Osasuna',             ARRAY['CA Osasuna','Club Atletico Osasuna'],                            'Spain', 'La Liga'),
('Rayo Vallecano',      ARRAY['Rayo','Rayo FC'],                                                'Spain', 'La Liga'),
('Real Madrid',         ARRAY['Madrid','Real','RMCF','Real Madrid CF'],                         'Spain', 'La Liga'),
('Real Sociedad',       ARRAY['La Real','Sociedad','Real Sociedad FC'],                         'Spain', 'La Liga'),
('Sevilla',             ARRAY['Sevilla FC','SFC'],                                              'Spain', 'La Liga'),
('Valencia',            ARRAY['Valencia CF','VCF'],                                             'Spain', 'La Liga'),
('Villarreal',          ARRAY['Villarreal CF','Yellow Submarine'],                              'Spain', 'La Liga'),
('Real Valladolid',     ARRAY['Valladolid','Real Valladolid CF'],                               'Spain', 'La Liga')
ON CONFLICT DO NOTHING;

-- ─── Serie A ─────────────────────────────────────────────────────────────────

INSERT INTO teams (name, aliases, country, league) VALUES
('AC Milan',        ARRAY['Milan','Rossoneri','ACM','AC Milan FC'],                             'Italy', 'Serie A'),
('Atalanta',        ARRAY['La Dea','Atalanta BC','Atalanta FC'],                               'Italy', 'Serie A'),
('Bologna',         ARRAY['Bologna FC','Bologna FC 1909'],                                      'Italy', 'Serie A'),
('Cagliari',        ARRAY['Cagliari Calcio','Cagliari FC'],                                    'Italy', 'Serie A'),
('Como',            ARRAY['Como 1907','Como FC'],                                               'Italy', 'Serie A'),
('Empoli',          ARRAY['Empoli FC','Empoli Calcio'],                                        'Italy', 'Serie A'),
('Fiorentina',      ARRAY['La Viola','ACF Fiorentina','Viola'],                                'Italy', 'Serie A'),
('Genoa',           ARRAY['Genoa CFC','Genoa FC'],                                             'Italy', 'Serie A'),
('Hellas Verona',   ARRAY['Verona','HV','Hellas Verona FC'],                                   'Italy', 'Serie A'),
('Inter Milan',     ARRAY['Inter','Internazionale','Inter FC','Nerazzurri','FC Internazionale'], 'Italy', 'Serie A'),
('Juventus',        ARRAY['Juve','Juventus FC','The Old Lady','Bianconeri'],                   'Italy', 'Serie A'),
('Lazio',           ARRAY['SS Lazio','Lazio FC'],                                              'Italy', 'Serie A'),
('Lecce',           ARRAY['US Lecce','Lecce FC'],                                              'Italy', 'Serie A'),
('Monza',           ARRAY['AC Monza','Monza FC'],                                              'Italy', 'Serie A'),
('Napoli',          ARRAY['SSC Napoli','Partenopei','Napoli FC'],                              'Italy', 'Serie A'),
('Parma',           ARRAY['Parma Calcio','Parma FC','Parma Calcio 1913'],                     'Italy', 'Serie A'),
('Roma',            ARRAY['AS Roma','Giallorossi','Roma FC'],                                  'Italy', 'Serie A'),
('Torino',          ARRAY['Torino FC','Toro','Granata'],                                       'Italy', 'Serie A'),
('Udinese',         ARRAY['Udinese Calcio','Udinese FC'],                                      'Italy', 'Serie A'),
('Venezia',         ARRAY['Venezia FC','Venezia Calcio'],                                      'Italy', 'Serie A')
ON CONFLICT DO NOTHING;

-- ─── Bundesliga ──────────────────────────────────────────────────────────────

INSERT INTO teams (name, aliases, country, league) VALUES
('FC Augsburg',                ARRAY['Augsburg','FCA'],                                         'Germany', 'Bundesliga'),
('Bayer Leverkusen',           ARRAY['Leverkusen','Werkself','Bayer 04'],                       'Germany', 'Bundesliga'),
('Bayern Munich',              ARRAY['Bayern','FC Bayern','FCB','Bayern München','Bayern Munchen'], 'Germany', 'Bundesliga'),
('Borussia Dortmund',          ARRAY['Dortmund','BVB','BVB 09'],                               'Germany', 'Bundesliga'),
('Borussia Monchengladbach',   ARRAY['Gladbach','BMG','Monchengladbach','Mönchengladbach'],     'Germany', 'Bundesliga'),
('Eintracht Frankfurt',        ARRAY['Frankfurt','SGE','Eintracht'],                            'Germany', 'Bundesliga'),
('SC Freiburg',                ARRAY['Freiburg','SC Freiburg'],                                 'Germany', 'Bundesliga'),
('TSG Hoffenheim',             ARRAY['Hoffenheim','TSG','1899 Hoffenheim'],                     'Germany', 'Bundesliga'),
('Holstein Kiel',              ARRAY['Kiel','Kiel FC'],                                         'Germany', 'Bundesliga'),
('1. FSV Mainz 05',            ARRAY['Mainz','Mainz 05','FSV Mainz'],                          'Germany', 'Bundesliga'),
('RB Leipzig',                 ARRAY['Leipzig','Red Bull Leipzig','RBL'],                       'Germany', 'Bundesliga'),
('FC St. Pauli',               ARRAY['St Pauli','Saint Pauli','FC St Pauli'],                  'Germany', 'Bundesliga'),
('VfB Stuttgart',              ARRAY['Stuttgart','VfB'],                                        'Germany', 'Bundesliga'),
('1. FC Union Berlin',         ARRAY['Union Berlin','Union','FC Union'],                        'Germany', 'Bundesliga'),
('Werder Bremen',              ARRAY['Bremen','SV Werder','Werder'],                            'Germany', 'Bundesliga'),
('VfL Wolfsburg',              ARRAY['Wolfsburg','VfL'],                                        'Germany', 'Bundesliga'),
('1. FC Heidenheim',           ARRAY['Heidenheim','FCH'],                                       'Germany', 'Bundesliga'),
('VfL Bochum',                 ARRAY['Bochum','VfL Bochum 1848'],                              'Germany', 'Bundesliga')
ON CONFLICT DO NOTHING;

-- ─── Ligue 1 ─────────────────────────────────────────────────────────────────

INSERT INTO teams (name, aliases, country, league) VALUES
('Angers',                  ARRAY['SCO Angers','Angers SCO'],                                  'France', 'Ligue 1'),
('Auxerre',                 ARRAY['AJ Auxerre','Auxerre FC'],                                  'France', 'Ligue 1'),
('Stade Brestois',          ARRAY['Brest','Stade Brest','Brest FC'],                          'France', 'Ligue 1'),
('Le Havre',                ARRAY['HAC','Le Havre AC','Le Havre FC'],                         'France', 'Ligue 1'),
('RC Lens',                 ARRAY['Lens','Lens FC'],                                           'France', 'Ligue 1'),
('Lille',                   ARRAY['Lille OSC','LOSC','LOSC Lille'],                           'France', 'Ligue 1'),
('Lyon',                    ARRAY['Olympique Lyonnais','OL','Olympique Lyon'],                'France', 'Ligue 1'),
('Marseille',               ARRAY['Olympique de Marseille','OM','Olympique Marseille'],       'France', 'Ligue 1'),
('AS Monaco',               ARRAY['Monaco','Monaco FC'],                                       'France', 'Ligue 1'),
('Montpellier',             ARRAY['Montpellier HSC','MHSC','Montpellier FC'],                 'France', 'Ligue 1'),
('FC Nantes',               ARRAY['Nantes','Nantes FC'],                                       'France', 'Ligue 1'),
('OGC Nice',                ARRAY['Nice','Nice FC'],                                           'France', 'Ligue 1'),
('Paris Saint-Germain',     ARRAY['PSG','Paris SG','Paris Saint Germain','Paris'],            'France', 'Ligue 1'),
('Stade Rennais',           ARRAY['Rennes','Stade de Rennes','Rennes FC'],                   'France', 'Ligue 1'),
('Stade de Reims',          ARRAY['Reims','Reims FC'],                                        'France', 'Ligue 1'),
('Saint-Etienne',           ARRAY['ASSE','Saint Etienne','St Etienne'],                       'France', 'Ligue 1'),
('RC Strasbourg',           ARRAY['Strasbourg','Strasbourg FC','Strasbourg Alsace'],          'France', 'Ligue 1'),
('Toulouse',                ARRAY['Toulouse FC','TFC'],                                        'France', 'Ligue 1')
ON CONFLICT DO NOTHING;
