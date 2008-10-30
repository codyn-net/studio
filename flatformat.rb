require 'saver'

module Cpg
	class FlatFormat
		def self.format(objects)
			s = "# CPG Network File\n"
			
			objects.select { |x| !x.is_a?(Components::Link) }.each do |o|
				s += "state\n#{o.get_property(:id)}\n"
				
				o.properties.each do |prop|
					next if prop == :id || prop == :display || o.invisible?(prop)
					
					s += "#{prop}\t#{o.initial_value(prop)}\t#{o.integrated?(prop) ? '1' : '0'}\n"
				end
				
				s += "\n"
			end
			
			objects.select { |x| x.is_a?(Components::Link) }.each do |o|
				s += "link\n#{o.from}\n#{o.to}\n"
				
				o.properties.each do |prop|
					next if prop == :id || prop == :label || o.invisible?(prop) || prop == :act_on || prop == :equation || prop == :from || prop == :to || prop == :display
					
					s += "#{prop}\t#{o.get_property(prop)}\t0\n"
				end
				
				s += "\n"
				
				vars = o.act_on.to_s.split(/\s*,\s*/)
				eq = o.equation.to_s.split(/\s*,\s*/, vars.length)
				
				eq.each_with_index do |e,idx|
					s += "#{vars[idx]}\t#{e}\n"
				end
				
				s += "\n"
			end
			
			s
		end
	end	
end
