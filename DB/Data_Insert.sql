BEGIN;

-- ============================================
-- 1. Базовые справочники
-- ============================================

INSERT INTO gamestore.country (name) VALUES
    ('USA'), ('Japan'), ('France'), ('Germany'), ('Sweden'),
    ('Poland'), ('UK'), ('Canada'), ('South Korea'), ('Russia'),
    ('Denmark'), ('Finland');

INSERT INTO gamestore.genre (name) VALUES
    ('Action'), ('RPG'), ('Strategy'), ('Shooter'), ('Adventure'),
    ('Indie'), ('Simulation'), ('Racing'), ('Sports'), ('Horror');

INSERT INTO gamestore."user" (login, wallet) VALUES
    ('gamer_pro', 100.50), ('pixel_master', 50.00), ('quest_lover', 200.00),
    ('speed_runner', 75.25), ('casual_player', 30.00), ('hardcore_fan', 500.00),
    ('indie_explorer', 120.00), ('mmo_veteran', 88.88), ('retro_gamer', 45.00),
    ('stream_watcher', 15.50);

-- ============================================
-- 2. Таблицы с внешними ключами
-- ============================================

INSERT INTO gamestore.publisher (name, country_id)
SELECT v.name, c.id
FROM (VALUES
    ('Valve',      	'USA'),
    ('Nintendo',   	'Japan'),
    ('Ubisoft',    	'France'),
    ('CD Projekt', 	'Poland'),
    ('Paradox',    	'Sweden'),
    ('EA',         	'USA'),
    ('Rockstar',   	'UK'),
    ('Nexon',      	'South Korea'),
	('Rusmir',	  	'Russia')
) AS v(name, country_name)
JOIN gamestore.country c ON c.name = v.country_name;

INSERT INTO gamestore.developer (name, country_id)
SELECT v.name, c.id
FROM (VALUES
    ('Valve Corp',     'USA'),
    ('CDPR',           'Poland'),
    ('Bethesda',       'USA'),
    ('FromSoftware',   'Japan'),
    ('Mojang',         'Sweden'),
    ('Epic Games',     'USA'),
    ('IO Interactive', 'Denmark'),
    ('Remedy',         'Finland'),
	('Siberian Code Studio', 'Russia'),
	('Rockstar Games',  'UK'),
	('Rockstar North',	'UK')
) AS v(name, country_name)
JOIN gamestore.country c ON c.name = v.country_name;

-- ============================================
-- 3. Игры
-- ============================================

INSERT INTO gamestore.game (title, description, date_release, price, system_required, publisher_id)
SELECT 
    v.title, v.description, v.date_release, v.price, v.system_required, p.id
FROM (VALUES
    ('Half-Life 2', 'Legendary FPS with physics-based gameplay', '2004-11-16'::date, 9.99, 'Windows 7, 2GB RAM, DirectX 9, 10GB HDD', 'Valve'),
    ('The Witcher 3', 'Open-world RPG in dark fantasy setting', '2015-05-19'::date, 39.99, 'Windows 10, 8GB RAM, GTX 770, 50GB SSD', 'CD Projekt'),
    ('Skyrim', 'Epic fantasy adventure in Tamriel', '2011-11-11'::date, 19.99, 'Windows 7, 4GB RAM, DirectX 9, 12GB HDD', 'Ubisoft'),
    ('Elden Ring', 'Action RPG from FromSoftware and George R.R. Martin', '2022-02-25'::date, 59.99, 'Windows 10, 12GB RAM, RTX 1060, 60GB SSD', 'Ubisoft'),
    ('Minecraft', 'Sandbox game with infinite possibilities', '2011-11-18'::date, 26.95, 'Any OS, 2GB RAM, Integrated GPU, 1GB HDD', 'Paradox'),
    ('Cyberpunk 2077', 'Open-world action-adventure in Night City', '2020-12-10'::date, 29.99, 'Windows 10, 12GB RAM, RTX 2060, 70GB SSD', 'CD Projekt'),
    ('Portal 2', 'Puzzle-platformer with cooperative mode', '2011-04-19'::date, 9.99, 'Windows 7, 2GB RAM, DirectX 9, 8GB HDD', 'Valve'),
    ('GTA V', 'Crime saga in Los Santos', '2013-09-17'::date, 29.99, 'Windows 10, 8GB RAM, GTX 1050, 100GB HDD', 'Rockstar'),
	('Slavic Quest', 'Приключенческая игра по мотивам славянской мифологии. Исследуйте древние леса, сражайтесь с мифическими существами и раскрывайте тайны предков.', '2024-03-15'::date, 7.99, 'Windows 10, 4 GB RAM, GTX 750 Ti, 10 GB', 'Rusmir')
) AS v(title, description, date_release, price, system_required, publisher_name)
JOIN gamestore.publisher p ON p.name = v.publisher_name;

-- ============================================
-- 4. Связующие таблицы
-- ============================================

INSERT INTO gamestore.game_developer (game_id, developer_id)
SELECT g.id, d.id
FROM (VALUES
    ('Half-Life 2',   	'Valve Corp'),
    ('The Witcher 3', 	'CDPR'),
    ('Skyrim',        	'Bethesda'),
    ('Elden Ring',    	'FromSoftware'),
    ('Minecraft',     	'Mojang'),
    ('Cyberpunk 2077',	'CDPR'),
    ('Portal 2',      	'Valve Corp'),
    ('GTA V',         	'Rockstar Games'), 
	('GTA V',         	'Rockstar North'),
	('Slavic Quest',	'Siberian Code Studio')	
) AS v(game_title, dev_name)
JOIN gamestore.game g ON g.title = v.game_title
JOIN gamestore.developer d ON d.name = v.dev_name;

INSERT INTO gamestore.game_genre (game_id, genre_id)
SELECT g.id, gen.id
FROM (VALUES
    ('Half-Life 2',   	'Shooter'),
    ('Half-Life 2',   	'Action'),
    ('The Witcher 3', 	'RPG'),
    ('The Witcher 3', 	'Adventure'),
    ('Skyrim',        	'RPG'),
    ('Elden Ring',    	'RPG'),
    ('Elden Ring',    	'Action'),
    ('Minecraft',     	'Indie'),
    ('Minecraft',     	'Simulation'),
    ('Cyberpunk 2077',	'RPG'),
    ('Portal 2',      	'Adventure'),
    ('GTA V',         	'Action'),
	('Slavic Quest',	'Adventure')
) AS v(game_title, genre_name)
JOIN gamestore.game g ON g.title = v.game_title
JOIN gamestore.genre gen ON gen.name = v.genre_name;

-- ============================================
-- 5. Версии игр
-- ============================================

INSERT INTO gamestore.game_version (game_id, date_release, description)
SELECT g.id, v.date_release, v.description
FROM (VALUES
    ('The Witcher 3', '2015-05-19'::date, 'Initial release'),
    ('The Witcher 3', '2016-05-31'::date, 'Hearts of Stone DLC included'),
    ('The Witcher 3', '2016-10-13'::date, 'Blood and Wine DLC + Next-Gen Update'),
    ('Cyberpunk 2077','2020-12-10'::date, 'Launch version'),
    ('Cyberpunk 2077','2023-09-26'::date, 'Update 2.0 + Phantom Liberty'),
    ('Minecraft',     '2011-11-18'::date, 'Full Release 1.0'),
    ('Minecraft',     '2023-06-07'::date, 'Trails & Tales Update 1.20')
) AS v(game_title, date_release, description)
JOIN gamestore.game g ON g.title = v.game_title;

-- ============================================
-- 6. Покупки пользователей
-- ============================================

INSERT INTO gamestore.game_user (game_id, user_id, date_purchase, price)
SELECT g.id, u.id, v.date_purchase, v.price
FROM (VALUES
    ('Half-Life 2',   'gamer_pro',      '2026-01-15'::date, 9.99),
    ('The Witcher 3', 'gamer_pro',      '2023-02-20'::date, 39.99),
    ('Minecraft',     'pixel_master',   CURRENT_DATE, 26.95),
    ('Skyrim',        'quest_lover',    '2023-04-05'::date, 19.99),
    ('Elden Ring',    'hardcore_fan',   '2024-05-12'::date, 59.99),
    ('Portal 2',      'indie_explorer', '2022-06-18'::date, 9.99),
    ('GTA V',         'speed_runner',   '2021-07-22'::date, 29.99),
    ('Cyberpunk 2077','mmo_veteran',    '2025-09-30'::date, 29.99)
) AS v(game_title, user_login, date_purchase, price)
JOIN gamestore.game g ON g.title = v.game_title
JOIN gamestore.user u ON u.login = v.user_login;

COMMIT;