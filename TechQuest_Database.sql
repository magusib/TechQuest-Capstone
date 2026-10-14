-- ============================================================
-- TECHQUEST DATABASE
-- PostgreSQL SQL Schema
-- ============================================================
-- Notes:
-- 1. Table/column names use lowercase snake_case to avoid PostgreSQL
--    reserved-word and case-sensitivity problems.
-- 2. Passwords and new_password should store HASHED passwords only.
-- 3. avatar/image fields are VARCHAR/TEXT paths or URLs, not binary files.
-- ============================================================

CREATE TABLE users (
    user_id         BIGSERIAL PRIMARY KEY,
    firstname       VARCHAR(100) NOT NULL,
    lastname        VARCHAR(100) NOT NULL,
    year_level      VARCHAR(30),
    email           VARCHAR(255) NOT NULL UNIQUE,
    password       TEXT NOT NULL,
    role            VARCHAR(20) NOT NULL
                    CHECK (role IN ('student', 'professor', 'admin')),
    avatar          TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- year_level is only allowed for students
    CONSTRAINT chk_users_year_level
        CHECK (
            (role = 'student' AND year_level IS NOT NULL)
            OR
            (role IN ('professor', 'admin') AND year_level IS NULL)
        )
);

CREATE TABLE email_verification (
    verification_id BIGSERIAL PRIMARY KEY,
    user_id         BIGINT NOT NULL,
    otp_code        VARCHAR(10) NOT NULL,
    expires_at      TIMESTAMPTZ NOT NULL,
    is_used         BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_email_verification_user
        FOREIGN KEY (user_id) REFERENCES users(user_id)
        ON DELETE CASCADE
);

CREATE TABLE password_reset (
    reset_id        BIGSERIAL PRIMARY KEY,
    user_id         BIGINT NOT NULL,
    otp_code        VARCHAR(10) NOT NULL,
    new_password    TEXT NOT NULL, -- store HASHED password only
    expires_at      TIMESTAMPTZ NOT NULL,
    is_used         BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_password_reset_user
        FOREIGN KEY (user_id) REFERENCES users(user_id)
        ON DELETE CASCADE
);

CREATE TABLE student_profile (
    student_id      BIGSERIAL PRIMARY KEY,
    user_id         BIGINT NOT NULL UNIQUE,
    total_coins     INTEGER NOT NULL DEFAULT 0 CHECK (total_coins >= 0),
    total_points    INTEGER NOT NULL DEFAULT 0 CHECK (total_points >= 0),

    CONSTRAINT fk_student_profile_user
        FOREIGN KEY (user_id) REFERENCES users(user_id)
        ON DELETE CASCADE
);

CREATE TABLE character (
    character_id    BIGSERIAL PRIMARY KEY,
    student_id      BIGINT NOT NULL UNIQUE,
    item_id         BIGINT NOT NULL,

    CONSTRAINT fk_character_student
        FOREIGN KEY (student_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE
);

CREATE TABLE shop_items (
    item_id         BIGSERIAL PRIMARY KEY,
    item_name       VARCHAR(150) NOT NULL,
    avatar_items    TEXT,
    character_items TEXT,
    price_coins     INTEGER NOT NULL DEFAULT 0 CHECK (price_coins >= 0),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE character
    ADD CONSTRAINT fk_character_item
    FOREIGN KEY (item_id) REFERENCES shop_items(item_id)
    ON DELETE RESTRICT;

CREATE TABLE user_inventory (
    inventory_id    BIGSERIAL PRIMARY KEY,
    student_id      BIGINT NOT NULL,
    item_id         BIGINT NOT NULL,
    purchased_at    TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_inventory_student
        FOREIGN KEY (student_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_inventory_item
        FOREIGN KEY (item_id) REFERENCES shop_items(item_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_inventory_student_item
        UNIQUE (student_id, item_id)
);

CREATE TABLE npc (
    npc_id          BIGSERIAL PRIMARY KEY,
    npc_name        VARCHAR(150) NOT NULL,
    npc_image       TEXT,
    difficulty      VARCHAR(20) NOT NULL DEFAULT 'Easy'
                    CHECK (LOWER(BTRIM(difficulty)) IN ('easy', 'medium', 'hard')),
    status          VARCHAR(30) NOT NULL DEFAULT 'active',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      BIGINT,

    CONSTRAINT fk_npc_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id)
        ON DELETE SET NULL
);

CREATE TABLE npc_deployment (
    deployment_id   BIGSERIAL PRIMARY KEY,
    npc_id          BIGINT NOT NULL,
    map_difficulty  VARCHAR(20) NOT NULL
                    CHECK (LOWER(BTRIM(map_difficulty)) IN ('easy', 'medium', 'hard')),
    position_x      REAL NOT NULL,
    position_y      REAL NOT NULL,
    position_marker VARCHAR(100) NOT NULL,
    status          VARCHAR(30) NOT NULL DEFAULT 'deployed',
    deployed_at     TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deployed_by     BIGINT,

    CONSTRAINT fk_npc_deployment_npc
        FOREIGN KEY (npc_id) REFERENCES npc(npc_id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_npc_deployment_user
        FOREIGN KEY (deployed_by) REFERENCES users(user_id)
        ON DELETE SET NULL
);

CREATE TABLE mini_games (
    mini_game_id    BIGSERIAL PRIMARY KEY,
    game_name       VARCHAR(150) NOT NULL,
    game_type       VARCHAR(50) NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      BIGINT,

    CONSTRAINT fk_minigame_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id)
        ON DELETE SET NULL
);

CREATE TABLE question (
    question_id     BIGSERIAL PRIMARY KEY,
    npc_id          BIGINT,
    mini_game_id    BIGINT,
    subject_name    VARCHAR(150) NOT NULL,
    question        TEXT NOT NULL,
    correct_answer  TEXT NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      BIGINT,

    CONSTRAINT fk_question_npc
        FOREIGN KEY (npc_id) REFERENCES npc(npc_id)
        ON DELETE SET NULL,

    CONSTRAINT fk_question_minigame
        FOREIGN KEY (mini_game_id) REFERENCES mini_games(mini_game_id)
        ON DELETE SET NULL,

    CONSTRAINT fk_question_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id)
        ON DELETE SET NULL
);

CREATE TABLE game_result (
    result_id       BIGSERIAL PRIMARY KEY,
    student_id      BIGINT NOT NULL,
    question_id     BIGINT NOT NULL,
    earned_points   INTEGER NOT NULL DEFAULT 0 CHECK (earned_points >= 0),
    earned_coins    INTEGER NOT NULL DEFAULT 0 CHECK (earned_coins >= 0),
    is_correct      BOOLEAN NOT NULL DEFAULT FALSE,
    played_at       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_game_result_student
        FOREIGN KEY (student_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_game_result_question
        FOREIGN KEY (question_id) REFERENCES question(question_id)
        ON DELETE RESTRICT
);

CREATE TABLE leaderboard (
    leaderboard_id  BIGSERIAL PRIMARY KEY,
    student_id      BIGINT NOT NULL UNIQUE,
    total_points    INTEGER NOT NULL DEFAULT 0 CHECK (total_points >= 0),
    rank_position   INTEGER CHECK (rank_position IS NULL OR rank_position > 0),

    CONSTRAINT fk_leaderboard_student
        FOREIGN KEY (student_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE
);

CREATE TABLE quest (
    quest_id        BIGSERIAL PRIMARY KEY,
    quest_title     VARCHAR(200) NOT NULL,
    description     TEXT,
    reward_points   INTEGER NOT NULL DEFAULT 0 CHECK (reward_points >= 0),
    reward_coins    INTEGER NOT NULL DEFAULT 0 CHECK (reward_coins >= 0),
    start_date      TIMESTAMPTZ NOT NULL,
    end_date        TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      BIGINT,

    CONSTRAINT fk_quest_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id)
        ON DELETE SET NULL,

    CONSTRAINT chk_quest_dates
        CHECK (end_date IS NULL OR end_date >= start_date)
);

CREATE TABLE student_quest (
    student_quest_id BIGSERIAL PRIMARY KEY,
    student_id       BIGINT NOT NULL,
    quest_id         BIGINT NOT NULL,
    status           VARCHAR(30) NOT NULL DEFAULT 'not_started',
    completed_at     TIMESTAMPTZ,

    CONSTRAINT fk_student_quest_student
        FOREIGN KEY (student_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_student_quest_quest
        FOREIGN KEY (quest_id) REFERENCES quest(quest_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_student_quest
        UNIQUE (student_id, quest_id)
);

CREATE TABLE multiplayer_room (
    room_id         BIGSERIAL PRIMARY KEY,
    room_code       VARCHAR(50) NOT NULL UNIQUE,
    room_name       VARCHAR(150) NOT NULL,
    create_at       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status          VARCHAR(30) NOT NULL DEFAULT 'waiting',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by       BIGINT,

    CONSTRAINT fk_room_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id)
        ON DELETE SET NULL
);

CREATE TABLE room_participants (
    participants_id BIGSERIAL PRIMARY KEY,
    room_id         BIGINT NOT NULL,
    user_id         BIGINT NOT NULL,
    joined_at       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_room_participant_room
        FOREIGN KEY (room_id) REFERENCES multiplayer_room(room_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_room_participant_student
        FOREIGN KEY (user_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_room_student
        UNIQUE (room_id, user_id)
);

CREATE TABLE professor_uploads (
    upload_id       BIGSERIAL PRIMARY KEY,
    professor_id    BIGINT NOT NULL,
    file_name       VARCHAR(255) NOT NULL,
    uploaded_at     TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      BIGINT,

    CONSTRAINT fk_professor_upload_professor
        FOREIGN KEY (professor_id) REFERENCES users(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_professor_upload_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id)
        ON DELETE SET NULL
);

CREATE TABLE room_result (
    room_result_id  BIGSERIAL PRIMARY KEY,
    room_id         BIGINT NOT NULL,
    student_id      BIGINT NOT NULL,
    score            INTEGER NOT NULL DEFAULT 0 CHECK (score >= 0),
    completed_at    TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_room_result_room
        FOREIGN KEY (room_id) REFERENCES multiplayer_room(room_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_room_result_student
        FOREIGN KEY (student_id) REFERENCES student_profile(student_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_room_result_student
        UNIQUE (room_id, student_id)
);

-- ============================================================
-- INDEXES
-- ============================================================

CREATE INDEX idx_email_verification_user_id ON email_verification(user_id);
CREATE INDEX idx_password_reset_user_id ON password_reset(user_id);
CREATE INDEX idx_question_npc_id ON question(npc_id);
CREATE INDEX idx_question_minigame_id ON question(mini_game_id);
CREATE INDEX idx_game_result_student_id ON game_result(student_id);
CREATE INDEX idx_game_result_question_id ON game_result(question_id);
CREATE INDEX idx_student_quest_student_id ON student_quest(student_id);
CREATE INDEX idx_student_quest_quest_id ON student_quest(quest_id);
CREATE INDEX idx_room_participants_room_id ON room_participants(room_id);
CREATE INDEX idx_room_participants_user_id ON room_participants(user_id);
CREATE INDEX idx_room_result_room_id ON room_result(room_id);
CREATE INDEX idx_room_result_student_id ON room_result(student_id);
CREATE INDEX idx_professor_uploads_professor_id ON professor_uploads(professor_id);

-- ============================================================
-- OPTIONAL: automatically keep leaderboard points synchronized
-- from student_profile is intentionally NOT added here.
-- Your application can update leaderboard after a game result.
-- ============================================================
