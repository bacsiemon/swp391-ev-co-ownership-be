# Group & Vehicle Management Integration Guide

## 🚗 Overview

This guide covers group management, vehicle co-ownership, member roles, voting systems, and fund management for the EV Co-Ownership platform.

## 🏗️ Group API Structure

Group endpoints support comprehensive vehicle sharing management:
- **Base URL**: `/api/Group`
- **Authentication**: Required for all endpoints
- **Permissions**: Role-based access (Owner, Member, Admin)

## 📋 Core Group Management

### 1. Group CRUD Operations

**List Groups**
```typescript
interface GroupListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: 'active' | 'inactive';
}

interface GroupDto {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
  memberCount: number;
  vehicleCount: number;
  status: 'active' | 'inactive';
  ownerUserId: number;
  totalFundBalance: number;
}

export const groupService = {
  async getGroups(query?: GroupListQuery): Promise<BaseResponse<GroupDto[]>> {
    const params = new URLSearchParams();
    if (query?.page) params.append('page', query.page.toString());
    if (query?.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query?.search) params.append('search', query.search);
    if (query?.status) params.append('status', query.status);
    
    return await apiClient.get(`/Group?${params.toString()}`);
  },

  async getGroup(id: number): Promise<BaseResponse<GroupDto>> {
    return await apiClient.get(`/Group/${id}`);
  },

  async createGroup(data: CreateGroupRequest): Promise<BaseResponse<GroupDto>> {
    return await apiClient.post('/Group', data);
  },

  async updateGroup(id: number, data: UpdateGroupRequest): Promise<BaseResponse<GroupDto>> {
    return await apiClient.put(`/Group/${id}`, data);
  },

  async deleteGroup(id: number): Promise<BaseResponse<void>> {
    return await apiClient.delete(`/Group/${id}`);
  }
};

// Group management component
const GroupManagement: React.FC = () => {
  const [groups, setGroups] = useState<GroupDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    loadGroups();
  }, [searchTerm]);

  const loadGroups = async () => {
    try {
      const response = await groupService.getGroups({ 
        search: searchTerm,
        page: 1,
        pageSize: 10 
      });
      
      if (response.statusCode === 200) {
        setGroups(response.data);
      }
    } catch (error) {
      console.error('Failed to load groups:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateGroup = async (groupData: CreateGroupRequest) => {
    try {
      const response = await groupService.createGroup(groupData);
      if (response.statusCode === 201) {
        toast.success('Group created successfully!');
        loadGroups();
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Failed to create group');
    }
  };

  return (
    <div className="group-management">
      <div className="page-header">
        <h2>Vehicle Groups</h2>
        <button onClick={() => setShowCreateModal(true)} className="create-btn">
          Create New Group
        </button>
      </div>

      <div className="search-filters">
        <input
          type="text"
          placeholder="Search groups..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="search-input"
        />
      </div>

      {loading ? (
        <div className="loading">Loading groups...</div>
      ) : (
        <div className="groups-grid">
          {groups.map(group => (
            <GroupCard 
              key={group.id} 
              group={group} 
              onUpdate={loadGroups}
            />
          ))}
        </div>
      )}
    </div>
  );
};
```

### 2. Group Creation & Updates

```typescript
interface CreateGroupRequest {
  name: string;
  description?: string;
}

interface UpdateGroupRequest extends CreateGroupRequest {}

const GroupForm: React.FC<{
  group?: GroupDto;
  onSubmit: (data: CreateGroupRequest) => void;
  onCancel: () => void;
}> = ({ group, onSubmit, onCancel }) => {
  const [formData, setFormData] = useState<CreateGroupRequest>({
    name: group?.name || '',
    description: group?.description || ''
  });

  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  const validateForm = (): boolean => {
    const newErrors: { [key: string]: string } = {};
    
    if (!formData.name.trim()) {
      newErrors.name = 'Group name is required';
    } else if (formData.name.length < 3) {
      newErrors.name = 'Group name must be at least 3 characters';
    }
    
    if (formData.description && formData.description.length > 500) {
      newErrors.description = 'Description must be less than 500 characters';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSubmit(formData);
    }
  };

  return (
    <div className="group-form">
      <h3>{group ? 'Update Group' : 'Create New Group'}</h3>
      
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Group Name *</label>
          <input
            type="text"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            placeholder="Enter group name"
            className={errors.name ? 'error' : ''}
          />
          {errors.name && <span className="error-text">{errors.name}</span>}
        </div>

        <div className="form-group">
          <label>Description</label>
          <textarea
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Describe the purpose of this group"
            rows={4}
            className={errors.description ? 'error' : ''}
          />
          {errors.description && <span className="error-text">{errors.description}</span>}
        </div>

        <div className="form-actions">
          <button type="button" onClick={onCancel} className="cancel-btn">
            Cancel
          </button>
          <button type="submit" className="submit-btn">
            {group ? 'Update Group' : 'Create Group'}
          </button>
        </div>
      </form>
    </div>
  );
};
```

## 👥 Member Management

### 1. Group Members

```typescript
interface GroupMemberDto {
  id: number;
  groupId: number;
  userId: number;
  role: 'Owner' | 'Member' | 'Admin';
  userName?: string;
  email?: string;
  joinedAt?: string;
  ownershipPercentage?: number;
  investmentAmount?: number;
  status: 'active' | 'pending' | 'inactive';
}

interface AddMemberRequest {
  userId: number;
  role: string;
  ownershipPercentage: number;
  investmentAmount: number;
}

interface UpdateMemberRoleRequest {
  role: string;
  ownershipPercentage?: number;
}

export const memberService = {
  async getGroupMembers(groupId: number): Promise<BaseResponse<GroupMemberDto[]>> {
    return await apiClient.get(`/Group/${groupId}/members`);
  },

  async addMember(groupId: number, memberData: AddMemberRequest): Promise<BaseResponse<GroupMemberDto>> {
    return await apiClient.post(`/Group/${groupId}/members`, memberData);
  },

  async removeMember(groupId: number, memberId: number): Promise<BaseResponse<void>> {
    return await apiClient.delete(`/Group/${groupId}/members/${memberId}`);
  },

  async updateMemberRole(groupId: number, memberId: number, roleData: UpdateMemberRoleRequest): Promise<BaseResponse<GroupMemberDto>> {
    return await apiClient.put(`/Group/${groupId}/members/${memberId}/role`, roleData);
  }
};

// Group members component
const GroupMembers: React.FC<{ groupId: number }> = ({ groupId }) => {
  const [members, setMembers] = useState<GroupMemberDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAddMember, setShowAddMember] = useState(false);

  useEffect(() => {
    loadMembers();
  }, [groupId]);

  const loadMembers = async () => {
    try {
      const response = await memberService.getGroupMembers(groupId);
      if (response.statusCode === 200) {
        setMembers(response.data);
      }
    } catch (error) {
      console.error('Failed to load members:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAddMember = async (memberData: AddMemberRequest) => {
    try {
      const response = await memberService.addMember(groupId, memberData);
      if (response.statusCode === 201) {
        toast.success('Member added successfully!');
        loadMembers();
        setShowAddMember(false);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to add member';
      toast.error(errorMessage);
    }
  };

  const handleRemoveMember = async (memberId: number) => {
    if (!confirm('Are you sure you want to remove this member?')) return;

    try {
      const response = await memberService.removeMember(groupId, memberId);
      if (response.statusCode === 200) {
        toast.success('Member removed successfully!');
        loadMembers();
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to remove member';
      toast.error(errorMessage);
    }
  };

  const handleUpdateRole = async (memberId: number, roleData: UpdateMemberRoleRequest) => {
    try {
      const response = await memberService.updateMemberRole(groupId, memberId, roleData);
      if (response.statusCode === 200) {
        toast.success('Member role updated successfully!');
        loadMembers();
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to update role';
      toast.error(errorMessage);
    }
  };

  if (loading) return <div className="loading">Loading members...</div>;

  return (
    <div className="group-members">
      <div className="members-header">
        <h3>Group Members ({members.length})</h3>
        <button 
          onClick={() => setShowAddMember(true)}
          className="add-member-btn"
        >
          Add Member
        </button>
      </div>

      <div className="members-list">
        {members.map(member => (
          <div key={member.id} className="member-card">
            <div className="member-info">
              <div className="member-name">
                {member.userName || `User ${member.userId}`}
              </div>
              <div className="member-details">
                <span className={`role-badge ${member.role.toLowerCase()}`}>
                  {member.role}
                </span>
                {member.ownershipPercentage && (
                  <span className="ownership">
                    {member.ownershipPercentage}% ownership
                  </span>
                )}
                <span className="join-date">
                  Joined: {member.joinedAt ? new Date(member.joinedAt).toLocaleDateString() : 'N/A'}
                </span>
              </div>
            </div>

            <div className="member-actions">
              <button 
                onClick={() => setEditingMember(member)}
                className="edit-btn"
              >
                Edit Role
              </button>
              <button 
                onClick={() => handleRemoveMember(member.id)}
                className="remove-btn"
                disabled={member.role === 'Owner'}
              >
                Remove
              </button>
            </div>
          </div>
        ))}
      </div>

      {showAddMember && (
        <AddMemberModal
          groupId={groupId}
          onAdd={handleAddMember}
          onCancel={() => setShowAddMember(false)}
        />
      )}
    </div>
  );
};
```

### 2. Add Member Modal

```typescript
const AddMemberModal: React.FC<{
  groupId: number;
  onAdd: (data: AddMemberRequest) => void;
  onCancel: () => void;
}> = ({ groupId, onAdd, onCancel }) => {
  const [formData, setFormData] = useState<AddMemberRequest>({
    userId: 0,
    role: 'Member',
    ownershipPercentage: 10,
    investmentAmount: 0
  });

  const [searchTerm, setSearchTerm] = useState('');
  const [searchResults, setSearchResults] = useState<UserSearchResult[]>([]);
  const [searching, setSearching] = useState(false);

  const searchUsers = async (term: string) => {
    if (term.length < 2) {
      setSearchResults([]);
      return;
    }

    setSearching(true);
    try {
      // Mock user search - replace with actual API call
      const mockResults: UserSearchResult[] = [
        { id: 1, name: 'John Doe', email: 'john@example.com', isCoOwner: true },
        { id: 2, name: 'Jane Smith', email: 'jane@example.com', isCoOwner: true }
      ];
      
      setSearchResults(mockResults.filter(user => 
        user.name.toLowerCase().includes(term.toLowerCase()) ||
        user.email.toLowerCase().includes(term.toLowerCase())
      ));
    } catch (error) {
      console.error('Failed to search users:', error);
    } finally {
      setSearching(false);
    }
  };

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (searchTerm) {
        searchUsers(searchTerm);
      }
    }, 300);

    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.userId) {
      toast.error('Please select a user');
      return;
    }

    if (formData.ownershipPercentage <= 0 || formData.ownershipPercentage > 100) {
      toast.error('Ownership percentage must be between 1 and 100');
      return;
    }

    onAdd(formData);
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h3>Add Member to Group</h3>
        
        <form onSubmit={handleSubmit}>
          {/* User Search */}
          <div className="form-group">
            <label>Search User *</label>
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search by name or email"
              className="search-input"
            />
            
            {searching && <div className="searching">Searching...</div>}
            
            {searchResults.length > 0 && (
              <div className="search-results">
                {searchResults.map(user => (
                  <div 
                    key={user.id} 
                    className={`search-result ${formData.userId === user.id ? 'selected' : ''}`}
                    onClick={() => setFormData({ ...formData, userId: user.id })}
                  >
                    <div className="user-info">
                      <span className="user-name">{user.name}</span>
                      <span className="user-email">{user.email}</span>
                    </div>
                    {user.isCoOwner && <span className="co-owner-badge">Co-Owner</span>}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Role Selection */}
          <div className="form-group">
            <label>Role *</label>
            <select
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value })}
            >
              <option value="Member">Member</option>
              <option value="Admin">Admin</option>
            </select>
          </div>

          {/* Ownership Percentage */}
          <div className="form-group">
            <label>Ownership Percentage *</label>
            <input
              type="number"
              min="1"
              max="100"
              value={formData.ownershipPercentage}
              onChange={(e) => setFormData({ 
                ...formData, 
                ownershipPercentage: parseInt(e.target.value) 
              })}
            />
          </div>

          {/* Investment Amount */}
          <div className="form-group">
            <label>Investment Amount (VND)</label>
            <input
              type="number"
              min="0"
              value={formData.investmentAmount}
              onChange={(e) => setFormData({ 
                ...formData, 
                investmentAmount: parseInt(e.target.value) 
              })}
              placeholder="0"
            />
          </div>

          <div className="modal-actions">
            <button type="button" onClick={onCancel} className="cancel-btn">
              Cancel
            </button>
            <button type="submit" className="submit-btn">
              Add Member
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
```

## 🚗 Vehicle Management

### 1. Group Vehicles

```typescript
interface GroupVehicleDto {
  id: number;
  groupId: number;
  name: string;
  brand: string;
  model: string;
  year: number;
  licensePlate: string;
  color: string;
  status: 'available' | 'in_use' | 'maintenance' | 'inactive';
  purchasePrice: number;
  currentValue?: number;
  totalCoOwners: number;
  fundBalance: number;
}

interface CreateVehicleRequest {
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  vinNumber: string;
  color: string;
  purchasePrice: number;
  description?: string;
}

export const groupVehicleService = {
  async getGroupVehicles(groupId: number): Promise<BaseResponse<GroupVehicleDto[]>> {
    return await apiClient.get(`/Group/${groupId}/vehicles`);
  },

  async getVehicleDetails(groupId: number, vehicleId: number): Promise<BaseResponse<VehicleDetailDto>> {
    return await apiClient.get(`/Group/${groupId}/vehicles/${vehicleId}`);
  },

  async createVehicle(groupId: number, vehicleData: CreateVehicleRequest): Promise<BaseResponse<GroupVehicleDto>> {
    return await apiClient.post(`/Group/${groupId}/vehicles`, vehicleData);
  },

  async getVehicleSchedule(groupId: number, vehicleId: number, startDate: string, endDate: string): Promise<BaseResponse<VehicleScheduleDto[]>> {
    return await apiClient.get(`/Group/${groupId}/vehicles/${vehicleId}/schedule?startDate=${startDate}&endDate=${endDate}`);
  }
};

// Group vehicles component
const GroupVehicles: React.FC<{ groupId: number }> = ({ groupId }) => {
  const [vehicles, setVehicles] = useState<GroupVehicleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);

  useEffect(() => {
    loadVehicles();
  }, [groupId]);

  const loadVehicles = async () => {
    try {
      const response = await groupVehicleService.getGroupVehicles(groupId);
      if (response.statusCode === 200) {
        setVehicles(response.data);
      }
    } catch (error) {
      console.error('Failed to load vehicles:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateVehicle = async (vehicleData: CreateVehicleRequest) => {
    try {
      const response = await groupVehicleService.createVehicle(groupId, vehicleData);
      if (response.statusCode === 201) {
        toast.success('Vehicle added to group successfully!');
        loadVehicles();
        setShowCreateModal(false);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to create vehicle';
      toast.error(errorMessage);
    }
  };

  if (loading) return <div className="loading">Loading vehicles...</div>;

  return (
    <div className="group-vehicles">
      <div className="vehicles-header">
        <h3>Group Vehicles ({vehicles.length})</h3>
        <button 
          onClick={() => setShowCreateModal(true)}
          className="add-vehicle-btn"
        >
          Add Vehicle
        </button>
      </div>

      {vehicles.length === 0 ? (
        <div className="no-vehicles">
          <div className="empty-state">
            <div className="empty-icon">🚗</div>
            <h4>No Vehicles Yet</h4>
            <p>Add your first vehicle to start co-ownership</p>
            <button 
              onClick={() => setShowCreateModal(true)}
              className="add-first-vehicle-btn"
            >
              Add First Vehicle
            </button>
          </div>
        </div>
      ) : (
        <div className="vehicles-grid">
          {vehicles.map(vehicle => (
            <VehicleCard 
              key={vehicle.id} 
              vehicle={vehicle} 
              groupId={groupId}
              onUpdate={loadVehicles}
            />
          ))}
        </div>
      )}

      {showCreateModal && (
        <CreateVehicleModal
          groupId={groupId}
          onCreate={handleCreateVehicle}
          onCancel={() => setShowCreateModal(false)}
        />
      )}
    </div>
  );
};
```

### 2. Vehicle Card Component

```typescript
const VehicleCard: React.FC<{
  vehicle: GroupVehicleDto;
  groupId: number;
  onUpdate: () => void;
}> = ({ vehicle, groupId, onUpdate }) => {
  const navigate = useNavigate();

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'available': return 'green';
      case 'in_use': return 'blue';
      case 'maintenance': return 'orange';
      case 'inactive': return 'red';
      default: return 'gray';
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  };

  return (
    <div className="vehicle-card">
      <div className="vehicle-header">
        <h4>{vehicle.name}</h4>
        <span className={`status-badge ${getStatusColor(vehicle.status)}`}>
          {vehicle.status.replace('_', ' ').toUpperCase()}
        </span>
      </div>

      <div className="vehicle-info">
        <div className="info-row">
          <span className="label">License Plate:</span>
          <span className="value">{vehicle.licensePlate}</span>
        </div>
        <div className="info-row">
          <span className="label">Year:</span>
          <span className="value">{vehicle.year}</span>
        </div>
        <div className="info-row">
          <span className="label">Co-owners:</span>
          <span className="value">{vehicle.totalCoOwners}</span>
        </div>
        <div className="info-row">
          <span className="label">Fund Balance:</span>
          <span className="value">{formatCurrency(vehicle.fundBalance)}</span>
        </div>
      </div>

      <div className="vehicle-actions">
        <button 
          onClick={() => navigate(`/group/${groupId}/vehicles/${vehicle.id}`)}
          className="view-details-btn"
        >
          View Details
        </button>
        <button 
          onClick={() => navigate(`/group/${groupId}/vehicles/${vehicle.id}/schedule`)}
          className="view-schedule-btn"
        >
          Schedule
        </button>
        <button 
          onClick={() => navigate(`/book-vehicle/${vehicle.id}`)}
          className="book-btn"
          disabled={vehicle.status !== 'available'}
        >
          Book
        </button>
      </div>
    </div>
  );
};
```

## 🗳️ Voting System

### 1. Group Votes

```typescript
interface GroupVoteDto {
  id: number;
  groupId: number;
  title: string;
  description: string;
  createdBy: number;
  createdAt: string;
  endDate?: string;
  isActive: boolean;
  voteType: 'maintenance' | 'expense' | 'policy' | 'member';
  options: VoteOption[];
  totalVotes: number;
  requiredVotes: number;
  currentUserVoted: boolean;
  result?: VoteResult;
}

interface VoteOption {
  id: number;
  text: string;
  voteCount: number;
  percentage: number;
}

interface VoteResult {
  winner: string;
  approved: boolean;
  finalizedAt: string;
}

interface CreateVoteRequest {
  title: string;
  description: string;
  voteType: string;
  options: string[];
  endDate?: string;
}

interface VoteSubmissionRequest {
  choice: string;
  comment?: string;
}

export const voteService = {
  async getGroupVotes(groupId: number): Promise<BaseResponse<GroupVoteDto[]>> {
    return await apiClient.get(`/Group/${groupId}/votes`);
  },

  async createVote(groupId: number, voteData: CreateVoteRequest): Promise<BaseResponse<GroupVoteDto>> {
    return await apiClient.post(`/Group/${groupId}/votes`, voteData);
  },

  async submitVote(groupId: number, voteId: number, voteData: VoteSubmissionRequest): Promise<BaseResponse<void>> {
    return await apiClient.post(`/Group/${groupId}/votes/${voteId}/vote`, voteData);
  }
};

// Group voting component
const GroupVoting: React.FC<{ groupId: number }> = ({ groupId }) => {
  const [votes, setVotes] = useState<GroupVoteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);

  useEffect(() => {
    loadVotes();
  }, [groupId]);

  const loadVotes = async () => {
    try {
      const response = await voteService.getGroupVotes(groupId);
      if (response.statusCode === 200) {
        setVotes(response.data);
      }
    } catch (error) {
      console.error('Failed to load votes:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateVote = async (voteData: CreateVoteRequest) => {
    try {
      const response = await voteService.createVote(groupId, voteData);
      if (response.statusCode === 201) {
        toast.success('Vote created successfully!');
        loadVotes();
        setShowCreateModal(false);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to create vote';
      toast.error(errorMessage);
    }
  };

  const handleSubmitVote = async (voteId: number, voteData: VoteSubmissionRequest) => {
    try {
      const response = await voteService.submitVote(groupId, voteId, voteData);
      if (response.statusCode === 200) {
        toast.success('Vote submitted successfully!');
        loadVotes();
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to submit vote';
      toast.error(errorMessage);
    }
  };

  if (loading) return <div className="loading">Loading votes...</div>;

  return (
    <div className="group-voting">
      <div className="voting-header">
        <h3>Group Votes</h3>
        <button 
          onClick={() => setShowCreateModal(true)}
          className="create-vote-btn"
        >
          Create Vote
        </button>
      </div>

      <div className="votes-filter">
        <button className="filter-btn active">All</button>
        <button className="filter-btn">Active</button>
        <button className="filter-btn">Completed</button>
        <button className="filter-btn">Maintenance</button>
      </div>

      <div className="votes-list">
        {votes.map(vote => (
          <VoteCard 
            key={vote.id} 
            vote={vote} 
            onVote={(voteData) => handleSubmitVote(vote.id, voteData)}
          />
        ))}
      </div>

      {showCreateModal && (
        <CreateVoteModal
          groupId={groupId}
          onCreate={handleCreateVote}
          onCancel={() => setShowCreateModal(false)}
        />
      )}
    </div>
  );
};
```

### 2. Vote Card Component

```typescript
const VoteCard: React.FC<{
  vote: GroupVoteDto;
  onVote: (voteData: VoteSubmissionRequest) => void;
}> = ({ vote, onVote }) => {
  const [selectedOption, setSelectedOption] = useState('');
  const [comment, setComment] = useState('');
  const [showVoteForm, setShowVoteForm] = useState(false);

  const handleSubmitVote = () => {
    if (!selectedOption) {
      toast.error('Please select an option');
      return;
    }

    onVote({
      choice: selectedOption,
      comment: comment.trim() || undefined
    });

    setShowVoteForm(false);
    setSelectedOption('');
    setComment('');
  };

  const getVoteProgress = () => {
    return vote.totalVotes > 0 ? (vote.totalVotes / vote.requiredVotes) * 100 : 0;
  };

  return (
    <div className="vote-card">
      <div className="vote-header">
        <h4>{vote.title}</h4>
        <div className="vote-meta">
          <span className={`vote-type ${vote.voteType}`}>
            {vote.voteType.toUpperCase()}
          </span>
          <span className={`vote-status ${vote.isActive ? 'active' : 'completed'}`}>
            {vote.isActive ? 'Active' : 'Completed'}
          </span>
        </div>
      </div>

      <div className="vote-description">
        <p>{vote.description}</p>
      </div>

      <div className="vote-progress">
        <div className="progress-bar">
          <div 
            className="progress-fill" 
            style={{ width: `${getVoteProgress()}%` }}
          />
        </div>
        <span className="progress-text">
          {vote.totalVotes} of {vote.requiredVotes} votes
        </span>
      </div>

      {vote.options.length > 0 && (
        <div className="vote-options">
          {vote.options.map(option => (
            <div key={option.id} className="vote-option">
              <div className="option-header">
                <span className="option-text">{option.text}</span>
                <span className="vote-count">{option.voteCount} votes ({option.percentage}%)</span>
              </div>
              <div className="option-bar">
                <div 
                  className="option-progress" 
                  style={{ width: `${option.percentage}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      )}

      {vote.result && (
        <div className="vote-result">
          <h5>Final Result:</h5>
          <p className={vote.result.approved ? 'approved' : 'rejected'}>
            {vote.result.winner} - {vote.result.approved ? 'APPROVED' : 'REJECTED'}
          </p>
          <small>Finalized on {new Date(vote.result.finalizedAt).toLocaleString()}</small>
        </div>
      )}

      <div className="vote-actions">
        {vote.isActive && !vote.currentUserVoted ? (
          <button 
            onClick={() => setShowVoteForm(true)}
            className="vote-btn"
          >
            Cast Vote
          </button>
        ) : vote.currentUserVoted ? (
          <span className="voted-indicator">✓ You voted</span>
        ) : (
          <span className="voting-closed">Voting closed</span>
        )}
      </div>

      {showVoteForm && (
        <div className="vote-form">
          <h5>Cast Your Vote</h5>
          
          <div className="vote-choices">
            {vote.options.map(option => (
              <label key={option.id} className="vote-choice">
                <input
                  type="radio"
                  name={`vote-${vote.id}`}
                  value={option.text}
                  checked={selectedOption === option.text}
                  onChange={(e) => setSelectedOption(e.target.value)}
                />
                <span>{option.text}</span>
              </label>
            ))}
          </div>

          <div className="comment-section">
            <label>Comments (optional)</label>
            <textarea
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Add your comments..."
              rows={3}
            />
          </div>

          <div className="form-actions">
            <button 
              onClick={() => setShowVoteForm(false)}
              className="cancel-btn"
            >
              Cancel
            </button>
            <button 
              onClick={handleSubmitVote}
              className="submit-vote-btn"
            >
              Submit Vote
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
```

## 💰 Fund Management

### 1. Group Fund

```typescript
interface GroupFundDto {
  groupId: number;
  balance: number;
  totalContributions: number;
  totalExpenses: number;
  currency: string;
  lastUpdated: string;
}

interface FundContributionRequest {
  amount: number;
  description: string;
  paymentMethod: string;
}

interface FundHistoryItem {
  id: number;
  type: 'contribution' | 'expense' | 'maintenance';
  amount: number;
  description: string;
  date: string;
  userId: number;
  userName: string;
  status: 'completed' | 'pending' | 'failed';
}

export const fundService = {
  async getGroupFund(groupId: number): Promise<BaseResponse<GroupFundDto>> {
    return await apiClient.get(`/Group/${groupId}/fund`);
  },

  async contributeFund(groupId: number, contribution: FundContributionRequest): Promise<BaseResponse<void>> {
    return await apiClient.post(`/Group/${groupId}/fund/contribute`, contribution);
  },

  async getFundHistory(groupId: number): Promise<BaseResponse<FundHistoryItem[]>> {
    return await apiClient.get(`/Group/${groupId}/fund/history`);
  }
};

// Group fund component
const GroupFund: React.FC<{ groupId: number }> = ({ groupId }) => {
  const [fund, setFund] = useState<GroupFundDto | null>(null);
  const [history, setHistory] = useState<FundHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [showContributeModal, setShowContributeModal] = useState(false);

  useEffect(() => {
    loadFundData();
  }, [groupId]);

  const loadFundData = async () => {
    try {
      const [fundResponse, historyResponse] = await Promise.all([
        fundService.getGroupFund(groupId),
        fundService.getFundHistory(groupId)
      ]);

      if (fundResponse.statusCode === 200) {
        setFund(fundResponse.data);
      }

      if (historyResponse.statusCode === 200) {
        setHistory(historyResponse.data);
      }
    } catch (error) {
      console.error('Failed to load fund data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleContribute = async (contribution: FundContributionRequest) => {
    try {
      const response = await fundService.contributeFund(groupId, contribution);
      if (response.statusCode === 201) {
        toast.success('Contribution added successfully!');
        loadFundData();
        setShowContributeModal(false);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to add contribution';
      toast.error(errorMessage);
    }
  };

  if (loading) return <div className="loading">Loading fund data...</div>;
  if (!fund) return <div className="error">Failed to load fund information</div>;

  return (
    <div className="group-fund">
      <div className="fund-overview">
        <h3>Group Fund</h3>
        
        <div className="fund-stats">
          <div className="stat-card">
            <h4>Current Balance</h4>
            <p className="balance">{formatCurrency(fund.balance)}</p>
          </div>
          
          <div className="stat-card">
            <h4>Total Contributions</h4>
            <p className="contributions">{formatCurrency(fund.totalContributions)}</p>
          </div>
          
          <div className="stat-card">
            <h4>Total Expenses</h4>
            <p className="expenses">{formatCurrency(fund.totalExpenses)}</p>
          </div>
        </div>

        <button 
          onClick={() => setShowContributeModal(true)}
          className="contribute-btn"
        >
          Contribute to Fund
        </button>
      </div>

      <div className="fund-history">
        <h4>Recent Transactions</h4>
        
        {history.length === 0 ? (
          <div className="no-history">
            <p>No transactions yet</p>
          </div>
        ) : (
          <div className="history-list">
            {history.map(item => (
              <div key={item.id} className="history-item">
                <div className="item-info">
                  <div className="item-header">
                    <span className={`item-type ${item.type}`}>
                      {item.type.toUpperCase()}
                    </span>
                    <span className={`item-status ${item.status}`}>
                      {item.status}
                    </span>
                  </div>
                  
                  <div className="item-details">
                    <p className="description">{item.description}</p>
                    <small className="metadata">
                      By {item.userName} on {new Date(item.date).toLocaleDateString()}
                    </small>
                  </div>
                </div>
                
                <div className={`item-amount ${item.type === 'contribution' ? 'positive' : 'negative'}`}>
                  {item.type === 'contribution' ? '+' : '-'}{formatCurrency(Math.abs(item.amount))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {showContributeModal && (
        <ContributeFundModal
          onContribute={handleContribute}
          onCancel={() => setShowContributeModal(false)}
        />
      )}
    </div>
  );
};
```

## 🎨 CSS Styling

```css
/* Group Management Styles */
.group-management {
  padding: 2rem;
}

.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-top: 2rem;
}

.group-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  transition: transform 0.2s;
}

.group-card:hover {
  transform: translateY(-2px);
}

/* Member Management */
.members-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.member-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
}

.role-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 16px;
  font-size: 0.75rem;
  font-weight: 600;
}

.role-badge.owner { background: #e3f2fd; color: #1976d2; }
.role-badge.admin { background: #fff3e0; color: #f57c00; }
.role-badge.member { background: #e8f5e8; color: #388e3c; }

/* Vehicle Grid */
.vehicles-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
}

.vehicle-card {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.status-badge.available { background: #e8f5e8; color: #388e3c; }
.status-badge.in_use { background: #e3f2fd; color: #1976d2; }
.status-badge.maintenance { background: #fff3e0; color: #f57c00; }
.status-badge.inactive { background: #ffebee; color: #d32f2f; }

/* Voting Styles */
.vote-card {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  margin-bottom: 1rem;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.progress-bar {
  width: 100%;
  height: 8px;
  background: #e0e0e0;
  border-radius: 4px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #2196f3;
  transition: width 0.3s ease;
}

.vote-option {
  margin: 0.5rem 0;
}

.option-bar {
  width: 100%;
  height: 6px;
  background: #f5f5f5;
  border-radius: 3px;
  overflow: hidden;
  margin-top: 0.25rem;
}

.option-progress {
  height: 100%;
  background: #4caf50;
  transition: width 0.3s ease;
}

/* Fund Management */
.fund-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin: 2rem 0;
}

.stat-card {
  background: white;
  padding: 1.5rem;
  border-radius: 12px;
  text-align: center;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.balance { color: #2196f3; font-size: 1.5rem; font-weight: bold; }
.contributions { color: #4caf50; font-size: 1.5rem; font-weight: bold; }
.expenses { color: #f44336; font-size: 1.5rem; font-weight: bold; }

.history-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  border-bottom: 1px solid #e0e0e0;
}

.item-amount.positive { color: #4caf50; font-weight: bold; }
.item-amount.negative { color: #f44336; font-weight: bold; }
```

---

**Next Step**: Tôi sẽ tiếp tục tạo file `README_FRONTEND_FILEUPLOAD.md` trong message tiếp theo để hoàn thành bộ README files.