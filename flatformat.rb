require 'saver'

module Cpg
	class FlatFormat
		class Object
			attr_reader :state, :parent, :node

			def initialize(node, parent)
				@node = node
				@state = node.state
				@parent = parent
			end
			
			def fullname
				parent ? "#{parent.get_property('id')}.#{node.get_property('id').to_s.gsub(/\./, '')}" : node.get_property('id').to_s
			end
		end
		
		class State < Object
			attr_reader :links
			
			def self.filter!(node, state)
				state.delete_if { |prop, val| prop == 'id' || prop == 'display' || node.invisible?(prop) }
			end
			
			def self.filter(node, state)
				state = state.dup
				self.filter!(node, state)
				
				state
			end

			def initialize(node, parent)
				super
				
				@links = node.links.dup
				
				State.filter!(node, @state)
			end
		end
		
		class Link < Object
			attr_accessor :from, :to

			def self.filter!(node, state)
				state.delete_if { |prop, val| ['id', 'label', 'act_on', 'equation', 'from', 'to', 'display'].include?(prop) || node.invisible?(prop) }
			end
			
			def self.filter(node, state)
				state = state.dup
				self.filter!(node, state)
				
				state
			end
			
			def initialize(node, parent)
				super
				
				@from = node.from
				@to = node.to
				
				Link.filter!(node, @state)
			end
		end
		
		def self.merge_with_parent(map, node, parent)
			# merge the state
			map[parent].state.merge!(State.filter(node, node.state))
			map[node] = map[parent]	
		end
		
		def self.flatten(map, node, parent = nil)
			res = []
			if node.is_a?(Components::Link)
				map[node] = Link.new(node, parent)
				res << map[node]
			else
				# group or state
				if parent && parent.main == node
					merge_with_parent(map, node, parent)
				else
					map[node] = State.new(node, parent)
					res << map[node]
				end
				
				if node.is_a?(Components::Group)
					node.children.each do |n|
						res += self.flatten(map, n, node)
					end
				end
			end
			
			res
		end
		
		def self.reformat(name)
			name.to_s.gsub(/\t+/, ' ')
		end

		def self.format(objects)
			s = "# CPG Network File\n"

			map = {}	
			res = []	
				
			objects.each do |obj|
				res += self.flatten(map, obj)
			end
			
			# map should now contain the full flattened structure
			res.select { |x| !x.is_a?(Link) }.each do |o|
				s += "state\n#{reformat(o.fullname)}\n"
				
				o.state.keys.each do |prop|
					s += "#{prop}\t#{reformat(o.node.initial_value(prop))}\t#{o.node.integrated?(prop) ? '1' : '0'}\n"
				end
				
				s += "\n"
			end
			
			res.select { |x| x.is_a?(Link) }.each do |o|
				s += "link\n#{reformat(o.fullname)}\n#{reformat(map[o.from].fullname)}\n#{reformat(map[o.to].fullname)}\n"
				
				o.state.keys.each do |prop|					
					s += "#{prop}\t#{reformat(o.node.get_property(prop))}\t0\n"
				end
				
				s += "\n"
				
				vars = o.node.act_on.to_s.split(/\s*,\s*/)
				eq = o.node.equation.to_s.split(/\s*,\s*/, vars.length)
				
				eq.each_with_index do |e,idx|
					s += "#{reformat(vars[idx])}\t#{e}\n"
				end
				
				s += "\n"
			end
			
			s
		end
	end	
end
