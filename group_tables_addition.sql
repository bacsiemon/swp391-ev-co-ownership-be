-- ==========================================================
-- THÊM CÁC BẢNG GROUP VÀO DATABASE
-- Để tương thích với GroupController đã tồn tại
-- ==========================================================
-- Bảng groups chính
CREATE TABLE groups (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    created_by INTEGER REFERENCES users(id),
    status_enum INTEGER DEFAULT 0,
    -- 0=Active, 1=Inactive, 2=Disbanded
    max_members INTEGER DEFAULT 10,
    group_type_enum INTEGER DEFAULT 0,
    -- 0=VehicleCoOwnership, 1=Community, 2=Business
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
-- Bảng thành viên nhóm
CREATE TABLE group_members (
    group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    role_enum INTEGER DEFAULT 0,
    -- 0=Member, 1=Admin, 2=Owner
    joined_at TIMESTAMP DEFAULT NOW(),
    investment_amount DECIMAL(15, 2) DEFAULT 0,
    ownership_percentage DECIMAL(5, 2) DEFAULT 0,
    status_enum INTEGER DEFAULT 0,
    -- 0=Active, 1=Pending, 2=Removed
    PRIMARY KEY (group_id, user_id)
);
-- Bảng xe thuộc nhóm
CREATE TABLE group_vehicles (
    group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
    vehicle_id INTEGER REFERENCES vehicles(id) ON DELETE CASCADE,
    added_at TIMESTAMP DEFAULT NOW(),
    status_enum INTEGER DEFAULT 0,
    -- 0=Active, 1=Maintenance, 2=Removed
    PRIMARY KEY (group_id, vehicle_id)
);
-- Bảng vote của nhóm
CREATE TABLE group_votes (
    id SERIAL PRIMARY KEY,
    group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    vote_type_enum INTEGER DEFAULT 0,
    -- 0=Maintenance, 1=Purchase, 2=Upgrade, 3=General
    created_by INTEGER REFERENCES users(id),
    start_time TIMESTAMP DEFAULT NOW(),
    end_time TIMESTAMP,
    status_enum INTEGER DEFAULT 0,
    -- 0=Active, 1=Completed, 2=Cancelled
    required_approval_percentage DECIMAL(5, 2) DEFAULT 60.00,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
-- Bảng vote responses
CREATE TABLE group_vote_responses (
    vote_id INTEGER REFERENCES group_votes(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    is_agree BOOLEAN NOT NULL,
    comments TEXT,
    voted_at TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (vote_id, user_id)
);
-- Bảng quỹ nhóm (tách biệt với funds chung)
CREATE TABLE group_funds (
    id SERIAL PRIMARY KEY,
    group_id INTEGER REFERENCES groups(id) ON DELETE CASCADE,
    fund_id INTEGER REFERENCES funds(id),
    -- Có thể link với fund chung hoặc tạo fund riêng
    target_amount DECIMAL(15, 2) DEFAULT 0,
    current_amount DECIMAL(15, 2) DEFAULT 0,
    purpose TEXT,
    deadline DATE,
    status_enum INTEGER DEFAULT 0,
    -- 0=Active, 1=Completed, 2=Cancelled
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
-- ==========================================================
-- INDEXES CHO HIỆU SUẤT
-- ==========================================================
CREATE INDEX idx_groups_created_by ON groups(created_by);
CREATE INDEX idx_groups_status ON groups(status_enum);
CREATE INDEX idx_group_members_group_id ON group_members(group_id);
CREATE INDEX idx_group_members_user_id ON group_members(user_id);
CREATE INDEX idx_group_members_role ON group_members(role_enum);
CREATE INDEX idx_group_vehicles_group_id ON group_vehicles(group_id);
CREATE INDEX idx_group_vehicles_vehicle_id ON group_vehicles(vehicle_id);
CREATE INDEX idx_group_votes_group_id ON group_votes(group_id);
CREATE INDEX idx_group_votes_status ON group_votes(status_enum);
CREATE INDEX idx_group_votes_created_by ON group_votes(created_by);
CREATE INDEX idx_group_vote_responses_vote_id ON group_vote_responses(vote_id);
CREATE INDEX idx_group_vote_responses_user_id ON group_vote_responses(user_id);
CREATE INDEX idx_group_funds_group_id ON group_funds(group_id);
CREATE INDEX idx_group_funds_fund_id ON group_funds(fund_id);
-- ==========================================================
-- DỮ LIỆU MẪU
-- ==========================================================
-- Tạo 2 nhóm mẫu
INSERT INTO groups (
        name,
        description,
        created_by,
        max_members,
        group_type_enum
    )
VALUES (
        'Tesla Co-ownership Group',
        'Group for sharing Tesla Model 3 ownership and costs',
        1,
        5,
        0
    ),
    (
        'VinFast Community',
        'Community group for VinFast VF8 co-owners',
        1,
        8,
        0
    );
-- Thêm thành viên vào nhóm
INSERT INTO group_members (
        group_id,
        user_id,
        role_enum,
        investment_amount,
        ownership_percentage
    )
VALUES (1, 1, 2, 0, 0),
    -- Admin là Owner nhóm 1
    (1, 3, 0, 800000, 55),
    -- John - Member với 55% ownership
    (1, 4, 0, 700000, 45),
    -- Jane - Member với 45% ownership
    (2, 1, 2, 0, 0),
    -- Admin là Owner nhóm 2  
    (2, 3, 0, 1200000, 60),
    -- John - Member với 60% ownership
    (2, 5, 0, 1100000, 40);
-- Mike - Member với 40% ownership
-- Link xe với nhóm
INSERT INTO group_vehicles (group_id, vehicle_id)
VALUES (1, 1),
    -- Tesla thuộc nhóm 1
    (2, 2);
-- VinFast thuộc nhóm 2
-- Tạo fund cho nhóm
INSERT INTO group_funds (
        group_id,
        fund_id,
        target_amount,
        current_amount,
        purpose
    )
VALUES (
        1,
        1,
        2000000,
        1500000,
        'Tesla Model 3 maintenance and operation fund'
    ),
    (
        2,
        2,
        3000000,
        2300000,
        'VinFast VF8 maintenance and upgrade fund'
    );
-- Tạo vote mẫu
INSERT INTO group_votes (
        group_id,
        title,
        description,
        vote_type_enum,
        created_by,
        end_time
    )
VALUES (
        1,
        'Approve Tesla Maintenance',
        'Vote to approve 2.5M VND maintenance cost for Tesla Model 3',
        0,
        1,
        NOW() + INTERVAL '7 days'
    ),
    (
        2,
        'VinFast Battery Upgrade',
        'Vote to upgrade VinFast battery capacity',
        2,
        1,
        NOW() + INTERVAL '10 days'
    );
-- Vote responses
INSERT INTO group_vote_responses (vote_id, user_id, is_agree, comments)
VALUES (1, 3, TRUE, 'Necessary maintenance for safety'),
    (1, 4, TRUE, 'Agree with the cost estimate'),
    (2, 3, FALSE, 'Too expensive for now'),
    (
        2,
        5,
        TRUE,
        'Worth the investment for longer range'
    );
COMMIT;